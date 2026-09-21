using System.ComponentModel.DataAnnotations;

namespace LocaSmart360.DTOs
{
    public class IngestaoDadosDTO
    {
        [Required(ErrorMessage = "A fonte de origem dos dados é obrigatória.")]
        public string Fonte { get; set; } = string.Empty;

        [Required(ErrorMessage = "O conteúdo bruto (Payload) não pode estar vazio.")]
        public string PayloadBruto { get; set; } = string.Empty;
    }
}