using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LocaSmart360.Models
{
    [Table("Dim_Usuarios")]
    public class Usuario
    {
        [Key]
        public Guid UsuarioId { get; set; } = Guid.NewGuid();

        [Required(ErrorMessage = "O nome completo é obrigatório para o cadastro.")]
        [StringLength(100, ErrorMessage = "O nome não pode exceder 100 caracteres.")]
        [Display(Name = "Nome do Administrador")]
        public string Nome { get; set; } = string.Empty;

        [Required(ErrorMessage = "O e-mail é obrigatório.")]
        [EmailAddress(ErrorMessage = "Por favor, insira um endereço de e-mail válido.")]
        [StringLength(150, ErrorMessage = "O e-mail não pode exceder 150 caracteres.")]
        [Display(Name = "E-mail Corporativo")]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(255)] 
        public string SenhaHash { get; set; } = string.Empty;

        [Required(ErrorMessage = "O nome da sua loja virtual é obrigatório.")]
        [StringLength(100, ErrorMessage = "O nome da loja não pode exceder 100 caracteres.")]
        [Display(Name = "Nome da Loja (Locaweb/Tray)")]
        public string NomeLoja { get; set; } = "Minha Loja Virtual";

        [Required(ErrorMessage = "O cargo do usuário é obrigatório.")]
        [StringLength(50)]
        [Display(Name = "Cargo / Nível de Acesso")]
        public string Cargo { get; set; } = "Lojista"; 

        [DataType(DataType.DateTime)]
        [Display(Name = "Data de Adesão")]
        public DateTime DataCadastro { get; set; } = DateTime.UtcNow;
    }
}