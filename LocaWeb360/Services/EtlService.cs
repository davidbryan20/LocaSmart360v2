using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using LocaSmart360.Data;
using LocaSmart360.Models;
using LocaSmart360.Services.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace LocaSmart360.Services
{
    public class EtlService : IEtlService
    {
        private readonly LocaSmartDbContext _context;
        private readonly IWebHostEnvironment _env;

        public EtlService(LocaSmartDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        private async Task<(bool Online, string DetalheErro)> EstaOnlineComDetalheAsync()
        {
            try
            {
                bool conectado = await _context.Database.CanConnectAsync();
                return (conectado, string.Empty);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        public async Task<(bool Sucesso, string Mensagem)> SalvarArquivoNoDataLakeAsync(IFormFile? arquivoJson, string nomePersonalizado, string? jsonTexto)
        {
            try
            {
                var (online, erroDetalhado) = await EstaOnlineComDetalheAsync();
                string statusModo = online
                    ? "Supabase (Online)"
                    : $"Fallback Local (Offline) | Motivo: {erroDetalhado}";

                var webRoot = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                var pastaDataLake = Path.Combine(webRoot, "DataLake", "Raw");

                if (!Directory.Exists(pastaDataLake))
                {
                    Directory.CreateDirectory(pastaDataLake);
                }

                var nomeLimpo = string.Join("_", nomePersonalizado.Split(Path.GetInvalidFileNameChars()));
                if (!nomeLimpo.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                {
                    nomeLimpo += ".json";
                }
                var caminhoCompleto = Path.Combine(pastaDataLake, nomeLimpo);

                if (arquivoJson != null && arquivoJson.Length > 0)
                {
                    using (var stream = new FileStream(caminhoCompleto, FileMode.Create))
                    {
                        await arquivoJson.CopyToAsync(stream);
                    }
                }
                else if (!string.IsNullOrWhiteSpace(jsonTexto))
                {
                    await File.WriteAllTextAsync(caminhoCompleto, jsonTexto);
                }
                else
                {
                    return (false, "Nenhum conteúdo fornecido.");
                }

                int totalProcessados = await ProcessarTodosArquivosPendentesNoDataLakeAsync();

                return (true, $"[Modo: {statusModo}] Arquivo salvo com sucesso! Varredura concluída: {totalProcessados} registros analisados.");
            }
            catch (Exception ex)
            {
                return (false, $"Erro ao salvar e processar: {ex.Message}");
            }
        }

        public async Task<int> ProcessarTodosArquivosPendentesNoDataLakeAsync()
        {
            int totalProcessados = 0;
            try
            {
                var webRoot = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                var pastaRaw = Path.Combine(webRoot, "DataLake", "Raw");
                var pastaProcessed = Path.Combine(webRoot, "DataLake", "Processed");

                if (!Directory.Exists(pastaRaw))
                {
                    return 0;
                }

                if (!Directory.Exists(pastaProcessed))
                {
                    Directory.CreateDirectory(pastaProcessed);
                }

                var arquivosJson = Directory.GetFiles(pastaRaw, "*.json", SearchOption.AllDirectories);

                foreach (var arquivo in arquivosJson)
                {
                    try
                    {
                        var jsonBruto = await File.ReadAllTextAsync(arquivo);
                        int processadosNesteArquivo = await ExecutarEtlManualAsync(jsonBruto);
                        totalProcessados += processadosNesteArquivo;

                        string nomeArquivo = Path.GetFileName(arquivo);
                        string destinoArquivo = Path.Combine(pastaProcessed, nomeArquivo);

                        if (File.Exists(destinoArquivo))
                        {
                            File.Delete(destinoArquivo);
                        }
                        File.Move(arquivo, destinoArquivo);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Erro ao processar o arquivo {Path.GetFileName(arquivo)}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro na varredura do DataLake: {ex.Message}");
            }

            return totalProcessados;
        }

        public async Task<int> ExecutarEtlManualAsync(string jsonBruto)
        {
            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var vendas = JsonSerializer.Deserialize<List<Venda>>(jsonBruto, options);

                if (vendas == null || !vendas.Any()) return 0;

                // CORREÇÃO CRUCIAL: Ordena o JSON cronologicamente para o histórico respeitar o tempo real
                vendas = vendas.OrderBy(v => v.DataVenda).ToList();

                var novasVendasParaAdicionar = new List<Venda>();
                var historicoPorCliente = new Dictionary<string, List<Venda>>();
                int contador = 0;

                foreach (var venda in vendas)
                {
                    if (venda.VendaId == Guid.Empty) venda.VendaId = Guid.NewGuid();

                    bool jaExiste = _context.Vendas.Local.Any(v => v.VendaId == venda.VendaId) ||
                                   await _context.Vendas.AnyAsync(v => v.VendaId == venda.VendaId) ||
                                   novasVendasParaAdicionar.Any(v => v.VendaId == venda.VendaId);

                    if (jaExiste) continue;

                    if (!historicoPorCliente.ContainsKey(venda.ClienteId))
                    {
                        var historicoDb = await _context.Vendas
                            .Where(v => v.ClienteId == venda.ClienteId)
                            .OrderByDescending(v => v.DataVenda)
                            .ToListAsync();

                        historicoPorCliente[venda.ClienteId] = historicoDb;
                    }

                    var historicoCliente = historicoPorCliente[venda.ClienteId];
                    var ultimaVenda = historicoCliente.FirstOrDefault();

                    var analise = await AnalisarRiscoAsync(venda, ultimaVenda, historicoCliente);
                    venda.ScoreRiscoFraude = analise.Score;
                    venda.JustificativaRisco = analise.Justificativa;

                    novasVendasParaAdicionar.Add(venda);
                    contador++;

                    // Insere no topo do cache local para manter consistência intra-lote
                    historicoCliente.Insert(0, venda);
                }

                if (novasVendasParaAdicionar.Any())
                {
                    await _context.Vendas.AddRangeAsync(novasVendasParaAdicionar);
                    await _context.SaveChangesAsync();
                }

                _context.EtlLogs.Add(new EtlLog { RegistrosProcessados = contador, Status = "Sucesso" });
                await _context.SaveChangesAsync();

                return contador;
            }
            catch (Exception ex)
            {
                _context.EtlLogs.Add(new EtlLog { RegistrosProcessados = 0, Status = "Falha", MensagemErro = ex.Message });
                await _context.SaveChangesAsync();
                throw;
            }
        }

        private async Task<(int Score, string Justificativa)> AnalisarRiscoAsync(Venda venda, Venda? ultima, List<Venda> historicoCliente)
        {
            int score = 0;
            List<string> motivos = new List<string>();

            bool ehMicrotransacao = venda.ValorTotal <= 15.00m;

            if (ultima != null)
            {
                double dist = CalcularDistancia((double)ultima.Lat, (double)ultima.Lon, (double)venda.Lat, (double)venda.Lon);
                double horas = (venda.DataVenda - ultima.DataVenda).TotalHours;

                if (horas > 0 && (dist / horas) > 900 && dist > 50)
                {
                    score += 75;
                    motivos.Add($"Deslocamento geográfico impossível ({dist:F0}km em {horas:F1}h).");
                }
                else if (horas == 0 && dist > 20)
                {
                    score += 75;
                    motivos.Add($"Transações simultâneas em localizações distintas ({dist:F0}km de distância).");
                }
            }

            if (!string.IsNullOrEmpty(venda.IpCliente))
            {
                var ultimaCompraMesmoIp = await _context.Vendas
                    .Where(v => v.IpCliente == venda.IpCliente)
                    .OrderByDescending(v => v.DataVenda)
                    .FirstOrDefaultAsync();

                if (ultimaCompraMesmoIp != null)
                {
                    double minutosIp = (venda.DataVenda - ultimaCompraMesmoIp.DataVenda).TotalMinutes;
                    if (minutosIp >= 0 && minutosIp < 10)
                    {
                        score += 75;
                        motivos.Add($"[VELOCITY IP] Múltiplas transações rápidas a partir do IP {venda.IpCliente} (última há {minutosIp:F1} min).");
                    }
                }
            }

            int horaLocal = venda.DataVenda.ToLocalTime().Hour;
            bool horarioRisco = horaLocal >= 0 && horaLocal <= 5;
            bool temHabitoNoturno = false;

            if (historicoCliente != null && historicoCliente.Any())
            {
                int comprasMadrugada = historicoCliente.Count(v => v.DataVenda.ToLocalTime().Hour >= 0 && v.DataVenda.ToLocalTime().Hour <= 5);
                double percentualMadrugada = (double)comprasMadrugada / historicoCliente.Count;
                if (percentualMadrugada >= 0.30) temHabitoNoturno = true;
            }

            // Análise de Micro-transação (Teste de Cartão)
            if (ehMicrotransacao)
            {
                score += 25;
                motivos.Add("Possível teste de cartão (Micro-transação).");
            }

            // Análise Avançada de Histórico, Cooldown e Pós-Micro-Transação
            if (historicoCliente != null && historicoCliente.Any())
            {
                var comprasValidas = historicoCliente.Where(v => v.ValorTotal > 15.00m).ToList();
                var ultimaCompraReal = comprasValidas.FirstOrDefault() ?? historicoCliente.First();
                decimal ticketMedio = comprasValidas.Any() ? comprasValidas.Average(v => v.ValorTotal) : historicoCliente.Average(v => v.ValorTotal);

                double horasDesdeUltima = (venda.DataVenda - historicoCliente.First().DataVenda).TotalHours;
                bool ultimaFoiMicro = historicoCliente.First().ValorTotal <= 15.00m;

                // CENÁRIO A: Cliente com histórico (3-4 compras) que sofreu micro-transação recente e tenta valor muito acima do ticket médio
                if (comprasValidas.Count >= 2 && ultimaFoiMicro && !ehMicrotransacao)
                {
                    if (venda.ValorTotal > (ticketMedio * 3.0m))
                    {
                        score += 100;
                        motivos.Add($"[GOLPE PÓS-MICRO] Compra alta (R$ {venda.ValorTotal:F2}) após teste de cartão, muito acima do ticket médio (R$ {ticketMedio:F2}). Bloqueio imediato.");
                    }
                    else if (venda.ValorTotal > (ticketMedio * 2.0m))
                    {
                        score += 50;
                        motivos.Add($"[ALERTA PÓS-MICRO] Compra acima da média após micro-transação (R$ {venda.ValorTotal:F2} vs média de R$ {ticketMedio:F2}).");
                    }
                }
                // CENÁRIO B: Cooldown estrito baseado em janelas de 24 horas e multiplicadores de valor
                else if (!ehMicrotransacao)
                {
                    decimal baseComparacao = ultimaCompraReal.ValorTotal;

                    if (horasDesdeUltima < 24.0)
                    {
                        // Menos de 24 horas: 2x = Suspeita, 3x ou mais = Bloqueio
                        if (venda.ValorTotal >= (baseComparacao * 3.0m))
                        {
                            score += 100;
                            motivos.Add($"[COOLDOWN <24h] Valor {venda.ValorTotal:C2} é 3x+ maior que o anterior ({baseComparacao:C2}) em menos de 24h ({horasDesdeUltima:F1}h). Bloqueio.");
                        }
                        else if (venda.ValorTotal >= (baseComparacao * 2.0m))
                        {
                            score += 50;
                            motivos.Add($"[COOLDOWN <24h] Valor {venda.ValorTotal:C2} é 2x maior que o anterior ({baseComparacao:C2}) em menos de 24h ({horasDesdeUltima:F1}h). Suspeito.");
                        }
                    }
                    else
                    {
                        // Mais de 24 horas: 3x = Suspeita, 4x ou mais = Bloqueio
                        if (venda.ValorTotal >= (baseComparacao * 4.0m))
                        {
                            score += 100;
                            motivos.Add($"[COOLDOWN >24h] Valor {venda.ValorTotal:C2} é 4x+ maior que o anterior ({baseComparacao:C2}) após 24h ({horasDesdeUltima:F1}h). Bloqueio.");
                        }
                        else if (venda.ValorTotal >= (baseComparacao * 3.0m))
                        {
                            score += 50;
                            motivos.Add($"[COOLDOWN >24h] Valor {venda.ValorTotal:C2} é 3x maior que o anterior ({baseComparacao:C2}) após 24h ({horasDesdeUltima:F1}h). Suspeito.");
                        }
                    }
                }
            }
            else
            {
                // Cold Start para clientes novos
                if (!ehMicrotransacao && venda.ValorTotal > 1000.00m)
                {
                    score += 100;
                    motivos.Add($"[COLD START] Primeira compra com valor superior ao limite de R$ 1.000,00 para clientes novos (Valor: R$ {venda.ValorTotal:F2}).");
                }
            }

            // Horário de risco
            if (horarioRisco && !ehMicrotransacao)
            {
                if (temHabitoNoturno)
                {
                    motivos.Add($"Operação de madrugada ({horaLocal}h) compatível com o hábito histórico do cliente.");
                }
                else
                {
                    score += 30;
                    motivos.Add($"Operação em horário de risco incomum para o perfil ({horaLocal}h).");
                }
            }

            score = Math.Min(score, 100);

            string justificativa = motivos.Count > 0 ? string.Join(" | ", motivos) : "Operação dentro do padrão comportamental e geográfico.";
            string nivelRisco = score >= 75 ? "CRÍTICO (BLOQUEADO)" : score >= 40 ? "ALERTA (SUSPEITO)" : "NORMAL";

            return (score, $"[{nivelRisco}] {justificativa}");
        }

        private double CalcularDistancia(double lat1, double lon1, double lat2, double lon2)
        {
            var R = 6371.0;
            var dLat = (lat2 - lat1) * (Math.PI / 180.0);
            var dLon = (lon2 - lon1) * (Math.PI / 180.0);
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(lat1 * (Math.PI / 180.0)) * Math.Cos(lat2 * (Math.PI / 180.0)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }
    }
}