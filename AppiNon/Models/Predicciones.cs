using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AppiNon.Models
{
    public class Predicciones
    {
        [Key]
        public int id_prediccion { get; set; }

        [Required]
        public int id_producto { get; set; }

        [Required]
        [Range(1, 12)]
        public int Mes { get; set; }

        [Required]
        public int Ano { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal ConsumoPredicho { get; set; }

        [Required]
        public int StockMinimoCalculado { get; set; }

        [Required]
        public int StockIdealCalculado { get; set; }

        [Required]
        [StringLength(20)]
        public string MetodoUsado { get; set; } // "General", "Mensual" o "Automatico"

        [Required]
        public DateTime FechaCalculo { get; set; } = DateTime.Now;

        // Relación con Producto
        [ForeignKey("id_producto")]
        public virtual Producto Producto { get; set; }
    }

    public class ParametrosSistema
    {
        [Key]
        public int id_parametro { get; set; }

        [Required]
        [StringLength(50)]
        public string Nombre { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal Valor { get; set; }

        [StringLength(200)]
        public string Descripcion { get; set; }
    }


}