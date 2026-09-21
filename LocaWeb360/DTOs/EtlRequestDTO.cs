using System.ComponentModel.DataAnnotations;

namespace LocaSmart360.DTOs
{
    public class EtlRequestDto
    {
        [Required(ErrorMessage = "O campo jsonBruto é obrigatório.")]
        public string JsonBruto { get; set; } = string.Empty;
    }
}