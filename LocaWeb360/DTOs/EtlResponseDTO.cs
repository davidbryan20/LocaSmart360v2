using System;

namespace LocaSmart360.DTOs
{
    public class EtlResponseDto
    {
        public bool Sucesso { get; set; }
        public string Mensagem { get; set; } = string.Empty;
        public int RegistrosProcessados { get; set; }
        public DateTime ExecutadoEm { get; set; } = DateTime.UtcNow;
    }
}