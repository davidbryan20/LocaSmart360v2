using System;

namespace LocaSmart360.DTOs
{
    public class ResultadoEtlDTO
    {
        public Guid ProcessoId { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Mensagem { get; set; } = string.Empty;
        public DateTime ExecutadoEm { get; set; } = DateTime.UtcNow;
    }
}