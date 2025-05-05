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
}
