using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LocaSmart360.Models
{
    [Table("Fato_Vendas")]
    public class Venda
    {
        [Key]
        public Guid VendaId { get; set; } = Guid.NewGuid();

        [Required(ErrorMessage = "O identificador do cliente comprador é obrigatório.")]
        [StringLength(100)]
        [Display(Name = "ID do Cliente Comprador")]
        public string ClienteId { get; set; } = string.Empty;

        [Required(ErrorMessage = "A vinculação de um produto é obrigatória.")]
        [Display(Name = "Produto Adquirido")]
        public Guid ProdutoId { get; set; }

        [ForeignKey("ProdutoId")]
        public Produto? Produto { get; set; }

        [Required(ErrorMessage = "O valor total da transação é obrigatório.")]
        [Range(0.01, 1000000.00, ErrorMessage = "O valor da venda deve ser maior que zero.")]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Valor Total (R$)")]
        public decimal ValorTotal { get; set; }

        [Required]
        [Range(0, 100)]
        [Display(Name = "Score de Risco Anti-Fraude (%)")]
        public int ScoreRiscoFraude { get; set; }

        [Required(ErrorMessage = "A justificativa da análise é obrigatória.")]
        [StringLength(500)]
        [Display(Name = "Justificativa da Análise de Risco")]
        public string JustificativaRisco { get; set; } = "Operação dentro do padrão esperado";

        [DataType(DataType.DateTime)]
        [Display(Name = "Data/Hora da Transação")]
        public DateTime DataVenda { get; set; } = DateTime.UtcNow;

        [Display(Name = "Latitude")]
        public double Lat { get; set; }

        [Display(Name = "Longitude")]
        public double Lon { get; set; }

        [StringLength(45)]
        [Display(Name = "IP do Cliente")]
        public string? IpCliente { get; set; }
    }
}