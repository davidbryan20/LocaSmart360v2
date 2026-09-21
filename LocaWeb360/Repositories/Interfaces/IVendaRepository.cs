using LocaSmart360.Models;

namespace LocaSmart360.Repositories.Interfaces
{
    public interface IVendaRepository
    {
        Task<IEnumerable<Venda>> ObterTodasComProdutosAsync();
        Task<IEnumerable<Venda>> ObterHistoricoPorClienteAsync(string clienteId);
        Task InserirVendaFatoAsync(Venda venda);
        Task<bool> SalvarAlteracoesAsync();
    }
}