using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using LocaSmart360.Services.Interfaces;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace LocaSmart360.Controllers
{
    public class EtlController : Controller
    {
        private readonly IEtlService _etlService;
        private readonly string _pastaDataLake;

        public EtlController(IEtlService etlService, IWebHostEnvironment env)
        {
            _etlService = etlService;

            // Alinhado para apontar exatamente para a subpasta Raw onde o serviço salva os arquivos
            var webRoot = env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            _pastaDataLake = Path.Combine(webRoot, "DataLake", "Raw");

            if (!Directory.Exists(_pastaDataLake))
            {
                Directory.CreateDirectory(_pastaDataLake);
            }
        }

        // 1. GET: /Vendas/Integracao - Exibe a tela de Integração
        [HttpGet]
        public IActionResult Integracao()
        {
            if (!Directory.Exists(_pastaDataLake))
            {
                Directory.CreateDirectory(_pastaDataLake);
            }

            return View("~/Views/Vendas/Integracao.cshtml");
        }

        
        [HttpGet]
        public IActionResult Index()
        {
            return RedirectToAction(nameof(Integracao));
        }

        // 2. POST: /Etl/SalvarNoDataLake - Processa Upload ou Texto Colado
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SalvarNoDataLake(IFormFile? arquivoJson, string nomeArquivoCustomizado, string? jsonDados, string tipoEntrada)
        {
            // Validação de cargo
            if (HttpContext.Session.GetString("UsuarioCargo") != "Administrador")
            {
                TempData["Erro"] = "Acesso Negado: Apenas administradores podem fazer ingestão de dados.";
                return RedirectToAction(nameof(Integracao));
            }

            if (string.IsNullOrWhiteSpace(nomeArquivoCustomizado))
            {
                TempData["Erro"] = "Por favor, informe um nome para o arquivo.";
                return RedirectToAction(nameof(Integracao));
            }

           
            var resultado = await _etlService.SalvarArquivoNoDataLakeAsync(arquivoJson, nomeArquivoCustomizado, jsonDados);

            if (resultado.Sucesso)
            {
                TempData["Sucesso"] = resultado.Mensagem;
            }
            else
            {
                TempData["Erro"] = resultado.Mensagem;
            }

            return RedirectToAction(nameof(Integracao));
        }

        // 3. GET: /Etl/Excluir?nomeArquivo=xxx
        [HttpGet]
        public IActionResult Excluir(string nomeArquivo)
        {
            if (!string.IsNullOrEmpty(nomeArquivo))
            {
                var caminho = Path.Combine(_pastaDataLake, nomeArquivo);
                if (System.IO.File.Exists(caminho))
                {
                    System.IO.File.Delete(caminho);
                    TempData["Sucesso"] = $"Arquivo '{nomeArquivo}' excluído com sucesso.";
                }
                else
                {
                    TempData["Erro"] = "Arquivo não encontrado.";
                }
            }
            return RedirectToAction(nameof(Integracao));
        }
    }
}