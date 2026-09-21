using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace LocaSmart360.Services.Interfaces
{
    public interface IEtlService
    {
        Task<(bool Sucesso, string Mensagem)> SalvarArquivoNoDataLakeAsync(IFormFile? arquivoJson, string nomePersonalizado, string? jsonTexto);
        Task<int> ExecutarEtlManualAsync(string jsonBruto);
        Task<int> ProcessarTodosArquivosPendentesNoDataLakeAsync();
    }
}