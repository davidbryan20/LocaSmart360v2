using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LocaSmart360.Models
{
    [Table("EtlLogs")]
    public class EtlLog
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public DateTime DataExecucao { get; set; } = DateTime.UtcNow;

        [Required]
        public int RegistrosProcessados { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = string.Empty; // "Sucesso" ou "Falha"

        public string? MensagemErro { get; set; }
    }
}