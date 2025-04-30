using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AppiNon.Models
{
    [Route("api/[controller]")]
    [ApiController]
    public class Producto : ControllerBase
    {
        public int id_producto { get; set; } 
        public string nombre_producto { get; set; }= null!;
        public int id_categoria { get; set; }
        public string unidad_medida { get; set; }
    }
    
//        public ICollection<Movimiento> Movimientos { get; set; }

          public class Inventario : ControllerBase
    {
        public int IdInventario { get; set; }
        public int IdProducto { get; set; }
        public int StockActual { get; set; }
        public int StockMinimo { get; set; }
        public int StockIdeal { get; set; }
        public DateTime UltimaEntrada { get; set; }
    }

    // 2. Modelo de Movimiento
    public class Movimiento : ControllerBase
    {
        public int IdMovimiento { get; set; }
        public int IdProducto { get; set; }
        public string TipoMovimiento { get; set; } // Entrada o Salida
        public int Cantidad { get; set; }
        public DateTime FechaMovimiento { get; set; }
        public Producto Producto { get; set; }
    }

}
