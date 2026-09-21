using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LocaSmart360.Models
{
    [Table("Dim_Produtos")]
    public class Produto
    {
        [Key]
        public Guid ProdutoId { get; set; } = Guid.NewGuid();

        [Required(ErrorMessage = "O código SKU do produto é obrigatório.")]
        [StringLength(50, ErrorMessage = "O SKU deve ter no máximo 50 caracteres.")]
        [Display(Name = "Código SKU")]
        public string Sku { get; set; } = string.Empty;

        [Required(ErrorMessage = "O nome do produto é obrigatório.")]
        [StringLength(255, ErrorMessage = "O nome do produto deve ter no máximo 255 caracteres.")]
        [Display(Name = "Nome do Produto")]
        public string Nome { get; set; } = string.Empty;

        [StringLength(100)]
        [Display(Name = "Categoria do Item")]
        public string Categoria { get; set; } = "Geral";

        [Display(Name = "Palavras-Chave Otimizadas para SEO")]
        public string PalavrasChaveSEO { get; set; } = string.Empty;
    }
}