using Microsoft.EntityFrameworkCore;
using LocaSmart360.Data;
using LocaSmart360.Models;
using LocaSmart360.Repositories.Interfaces;
using System.Text.Json;

namespace LocaSmart360.Repositories
{
    public class VendaRepository : IVendaRepository
    {
        private readonly LocaSmartDbContext _context;
        private readonly IWebHostEnvironment _env;

        public VendaRepository(LocaSmartDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // Traz todas as vendas unificando o Supabase e a pasta DataLake (com busca em subpastas)
        public async Task<IEnumerable<Venda>> ObterTodasComProdutosAsync()
        {
            var todasAsVendas = new List<Venda>();

            // 1. BUSCA DO BANCO DE DADOS (SUPABASE)
            try
            {
                var vendasBanco = await _context.Vendas
                    .Include(v => v.Produto)
                    .ToListAsync();

                if (vendasBanco != null)
                {
                    todasAsVendas.AddRange(vendasBanco);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Aviso: Não foi possível carregar do Supabase: {ex.Message}");
            }

            // 2. BUSCA DA PASTA DATALAKE (ARQUIVOS JSON LOCAIS)
            try
            {
                var pastaDataLake = Path.Combine(_env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "DataLake");

                if (Directory.Exists(pastaDataLake))
                {
                    foreach (var arquivo in Directory.GetFiles(pastaDataLake, "*.json", SearchOption.AllDirectories))
                    {
                        try
                        {
                            var jsonBruto = await File.ReadAllTextAsync(arquivo);
                            var opcoes = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                            var dadosDoArquivo = JsonSerializer.Deserialize<List<Venda>>(jsonBruto, opcoes);

                            if (dadosDoArquivo != null)
                            {
                                todasAsVendas.AddRange(dadosDoArquivo);
                            }
                        }
                        catch
                        {
                            // Ignora arquivos que não correspondam à estrutura de vendas
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Aviso: Erro ao ler arquivos do DataLake: {ex.Message}");
            }

            // 3. REMOVE DUPLICADAS (caso o pedido esteja no banco e no JSON) E ORDENA
            return todasAsVendas
                .GroupBy(v => v.VendaId)
                .Select(g => g.First())
                .OrderByDescending(v => v.DataVenda);
        }

        // Busca o histórico de um cliente específico combinando banco e arquivos locais
        public async Task<IEnumerable<Venda>> ObterHistoricoPorClienteAsync(string clienteId)
        {
            try
            {
                var historicoBanco = await _context.Vendas
                    .Where(v => v.ClienteId == clienteId)
                    .ToListAsync();

                if (historicoBanco.Any())
                {
                    return historicoBanco;
                }
            }
            catch (Exception)
            {
                // Ignora e tenta o fallback unificado abaixo
            }

            // Fallback unificado caso o banco falhe na consulta específica
            var vendasGerais = await ObterTodasComProdutosAsync();
            return vendasGerais.Where(v => v.ClienteId == clienteId).ToList();
        }

        // Insere o registro na tabela fato
        public async Task InserirVendaFatoAsync(Venda venda)
        {
            try
            {
                await _context.Vendas.AddAsync(venda);
            }
            catch (Exception)
            {
                // Modo offline: se o banco estiver indisponível na inserção manual, apenas ignora
            }
        }

        // Salva de fato todas as transações pendentes no Supabase
        public async Task<bool> SalvarAlteracoesAsync()
        {
            try
            {
                return (await _context.SaveChangesAsync()) > 0;
            }
            catch (Exception)
            {
                // Retorna true de forma simulada no modo offline para manter o fluxo
                return true;
            }
        }
    }
}