using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using LocaSmart360.Models;
using LocaSmart360.Repositories.Interfaces;
using LocaSmart360.Services.Interfaces;
using LocaSmart360.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.Text.Json;
using System.Data;

namespace LocaSmart360.Controllers
{
    public class VendasController : Controller
    {
        private readonly IVendaRepository _vendaRepository;
        private readonly LocaSmartDbContext _context;
        private readonly IEtlService _etlService;
        private readonly IWebHostEnvironment _env;

        public VendasController(IVendaRepository vendaRepository, LocaSmartDbContext context, IEtlService etlService, IWebHostEnvironment env)
        {
            _vendaRepository = vendaRepository;
            _context = context;
            _etlService = etlService;
            _env = env;
        }

        private async Task<bool> EstaOnlineAsync()
        {
            try
            {
                var connection = _context.Database.GetDbConnection();
                if (connection.State != ConnectionState.Open)
                {
                    await connection.OpenAsync();
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<IActionResult> Index()
        {
            if (HttpContext.Session.GetString("UsuarioId") == null)
                return RedirectToAction("Login", "Autenticacao");

            bool isOnline = await EstaOnlineAsync();
            ViewBag.SistemaOnline = isOnline;

            if (isOnline)
            {
                var vendas = await _vendaRepository.ObterTodasComProdutosAsync();
                return View(vendas);
            }
            else
            {
                var vendasOffline = LerVendasDoDataLakeLocal();
                return View(vendasOffline);
            }
        }

        public async Task<IActionResult> Auditoria()
        {
            if (HttpContext.Session.GetString("UsuarioId") == null)
                return RedirectToAction("Login", "Autenticacao");

            bool isOnline = await EstaOnlineAsync();
            ViewBag.SistemaOnline = isOnline;

            List<Venda> vendas;
            if (isOnline)
            {
                vendas = (await _vendaRepository.ObterTodasComProdutosAsync()).ToList();
            }
            else
            {
                vendas = LerVendasDoDataLakeLocal();
            }

            return View(vendas);
        }

        private List<Venda> LerVendasDoDataLakeLocal()
        {
            var listaVendas = new List<Venda>();
            try
            {
                var webRoot = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                var pastaRaw = Path.Combine(webRoot, "DataLake", "Raw");

                if (Directory.Exists(pastaRaw))
                {
                    var arquivos = Directory.GetFiles(pastaRaw, "*.json", SearchOption.AllDirectories);
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

                    foreach (var arq in arquivos)
                    {
                        var json = System.IO.File.ReadAllText(arq);
                        var itens = JsonSerializer.Deserialize<List<Venda>>(json, options);
                        if (itens != null) listaVendas.AddRange(itens);
                    }
                }
            }
            catch { }
            return listaVendas.OrderByDescending(v => v.DataVenda).ToList();
        }

        [HttpGet]
        public async Task<JsonResult> ObterDetalhes(Guid id)
        {
            if (await EstaOnlineAsync())
            {
                var venda = await _context.Vendas.Include(v => v.Produto).FirstOrDefaultAsync(v => v.VendaId == id);
                if (venda == null) return Json(new { error = "Registro não encontrado" });

                return Json(new
                {
                    vendaId = venda.VendaId.ToString(),
                    id = venda.VendaId.ToString().Substring(0, 8) + "...",
                    clienteId = venda.ClienteId,
                    produto = venda.Produto?.Nome ?? "Produto Indisponível",
                    valorTotal = venda.ValorTotal,
                    dataVenda = venda.DataVenda,
                    scoreRiscoFraude = venda.ScoreRiscoFraude,
                    justificativaRisco = venda.JustificativaRisco
                });
            }
            return Json(new { error = "Sistema offline (Modo Fallback ativado)." });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Criar(Venda novaVenda)
        {
            if (!await EstaOnlineAsync())
            {
                TempData["ErroETL"] = "Não é possível cadastrar novas vendas com o banco offline.";
                return RedirectToAction(nameof(Auditoria));
            }

            var historico = await _vendaRepository.ObterHistoricoPorClienteAsync(novaVenda.ClienteId);
            int score = 0;
            if (historico.Any())
            {
                decimal ticketMedio = historico.Average(v => v.ValorTotal);
                if (novaVenda.ValorTotal > (ticketMedio * 3.0m)) score += 50;
            }
            else score += 15;

            int hora = DateTime.UtcNow.AddHours(-3).Hour;
            if (hora >= 0 && hora <= 5) score += 35;

            novaVenda.ScoreRiscoFraude = Math.Min(score, 100);
            novaVenda.DataVenda = DateTime.UtcNow;

            List<string> motivos = new List<string>();
            if (historico.Any())
            {
                decimal ticketMedio = historico.Average(v => v.ValorTotal);
                if (novaVenda.ValorTotal > (ticketMedio * 3.0m)) motivos.Add("Valor 3x superior ao ticket médio.");
            }
            else { motivos.Add("Primeira compra do cliente."); }
            if (hora >= 0 && hora <= 5) motivos.Add($"Transação fora do horário comercial ({hora}h).");

            novaVenda.JustificativaRisco = motivos.Any() ? string.Join(" | ", motivos) : "Transação dentro dos parâmetros de normalidade.";

            if (ModelState.IsValid)
            {
                await _vendaRepository.InserirVendaFatoAsync(novaVenda);
                await _vendaRepository.SalvarAlteracoesAsync();
                return RedirectToAction(nameof(Auditoria));
            }
            return RedirectToAction(nameof(Auditoria));
        }

        public async Task<IActionResult> Integracao() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessarETL(string tipoEntrada, IFormFile arquivoJson, string jsonDados, string nomeArquivoCustomizado)
        {
            try
            {
                string conteudoFinal = string.Empty;

                if (tipoEntrada == "arquivo" && arquivoJson != null && arquivoJson.Length > 0)
                {
                    using var reader = new StreamReader(arquivoJson.OpenReadStream());
                    conteudoFinal = await reader.ReadToEndAsync();
                }
                else if (tipoEntrada == "texto" && !string.IsNullOrEmpty(jsonDados))
                {
                    conteudoFinal = jsonDados;
                }
                else
                {
                    TempData["ErroETL"] = "Nenhum dado válido foi fornecido para o pipeline.";
                    return RedirectToAction("Integracao");
                }

                var webRoot = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                var pastaRaw = Path.Combine(webRoot, "DataLake", "Raw");
                var pastaProcessados = Path.Combine(webRoot, "DataLake", "Processados");

                if (!Directory.Exists(pastaRaw)) Directory.CreateDirectory(pastaRaw);
                if (!Directory.Exists(pastaProcessados)) Directory.CreateDirectory(pastaProcessados);

                string nomeArquivoFinal = string.IsNullOrWhiteSpace(nomeArquivoCustomizado)
                    ? $"carga_{DateTime.UtcNow:yyyyMMdd_HHmmss}.json"
                    : $"{nomeArquivoCustomizado.Trim()}.json";

                string caminhoRaw = Path.Combine(pastaRaw, nomeArquivoFinal);
                await System.IO.File.WriteAllTextAsync(caminhoRaw, conteudoFinal);

                int processados = await _etlService.ExecutarEtlManualAsync(conteudoFinal);

                string caminhoProcessado = Path.Combine(pastaProcessados, nomeArquivoFinal);
                if (System.IO.File.Exists(caminhoProcessado)) System.IO.File.Delete(caminhoProcessado);
                System.IO.File.Move(caminhoRaw, caminhoProcessado);

                TempData["SucessoETL"] = $"Sucesso! Arquivo processado e {processados} transações gravadas no Supabase.";
            }
            catch (Exception ex)
            {
                TempData["ErroETL"] = $"Falha no pipeline ETL: {ex.Message}";
            }

            return RedirectToAction("Integracao");
        }
    }
}