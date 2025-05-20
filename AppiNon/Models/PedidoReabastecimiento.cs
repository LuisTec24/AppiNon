using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace AppiNon.Models
{
    public class PedidoReabastecimiento
    {
        public int IdPedido { get; set; }
        public int IdProducto { get; set; }
        public int Cantidad { get; set; }
        public DateTime FechaSolicitud { get; set; }
        public DateTime? FechaRecepcion { get; set; }
        public string Estado { get; set; } // "Pendiente", "Enviado", "Recibido"
    }

    // DTO para creación de inventario
    public class CrearInventarioDto
    {
        public int IdProducto { get; set; }
        public int StockActual { get; set; }
        public int StockMinimo { get; set; }
        public int StockIdeal { get; set; }
    }

    public class ProductoUpdateDto
    {
        public int Id_producto { get; set; }  // Debe coincidir con el id en ruta
        public string Nombre_producto { get; set; }
        public int Id_categoria { get; set; }
        public string Unidad_medida { get; set; }
        public int Id_provedor { get; set; }
        public bool Reabastecimientoautomatico { get; set; }
        public string MetodoPrediccion { get; set; }
    }

    public class ProductoCreateDto
    {
        public string Nombre_producto { get; set; }
        public int Id_categoria { get; set; }
        public string Unidad_medida { get; set; }
        public int Id_Provedor { get; set; }
        public bool Reabastecimientoautomatico { get; set; }
        public string MetodoPrediccion { get; set; }
    }
}
