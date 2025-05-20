using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations.Schema;

namespace AppiNon.Models
{
    [Route("api/[controller]")]
    [ApiController]

    public class Producto
    {
        public int Id_producto { get; set; }
        public string Nombre_producto { get; set; } = null!;
        public int Id_categoria { get; set; }
        public string Unidad_medida { get; set; } = null!;
        public int Id_provedor { get; set; }
        public bool Reabastecimientoautomatico { get; set; } = true; // Nuevo campo
       

        public virtual Inventario Inventario { get; set; }
        public string? Metodoprediccion { get; set; } = null!;
        //  public ICollection<Producto> Categoria { get; set; }
    }

    public class Categorias
    {
        public int id_categoria { get; set; }
        public string Categoria { get; set; } = null!;

    }

    //        public ICollection<Movimiento> Movimientos { get; set; }

    public class Inventario
    {
        [Column("id_inventario")]  // Mapea EXACTAMENTE al nombre en BD
        public int IdInventario { get; set; }
        [Column("id_producto")]    // Mapea EXACTAMENTE al nombre en BD
        public int IdProducto { get; set; }
        public int StockActual { get; set; }
        public int StockMinimo { get; set; }
        public int StockIdeal { get; set; }
        public DateTime UltimaEntrada { get; set; }
        public virtual Producto Producto { get; set; } // navegación inversa
                                                       //
                                                       //
    }


    public class Roles
    {
     public int ID { get; set; }
    public string Nombre_rol { get; set; }
        public string Descripcion { get; set; }
    }


    public class Proveedores
    {
        public int ID_proveedor { get; set; }
        public string Nombre_Proveedor { get; set; } = null!;
        public int Tiempo_entrega_dias { get; set; }
        public string Telefono { get; set; }

    }

    public class Bitacora
    {
        public int ID { get; set; }
        public DateTime Fecha { get; set; }
        //public string Tipo_de_Modificacion { get; set; } = null!;
     //   public int ID_Usuario { get; set; }
     //   public string Entidad { get; set; }
       // public int ID_Entidad { get; set; }
       // public string Descripcion { get; set; } = null!;
        public string? Tipo_de_Modificacion { get; set; }
        public string? Entidad { get; set; }
        public string? Descripcion { get; set; }
        public int? ID_Usuario { get; set; }
        public int? ID_Entidad { get; set; }


    }

    public class Pedido
    {
        public int IdPedido { get; set; }
        public int IdProducto { get; set; }
        public int Cantidad { get; set; }
        public DateTime FechaSolicitud { get; set; } = DateTime.Now;
        public DateTime? FechaRecepcion { get; set; }
        public string Estado { get; set; } = "Pendiente";
        public int IdProveedor { get; set; }
        public string? SolicitadoPor { get; set; } // Usuario que lo solicitó (para manuales)

        [ForeignKey("IdProducto")]
        public virtual Producto Producto { get; set; } 

        [ForeignKey("IdProveedor")]
        public virtual Proveedores Proveedor { get; set; } 
 
        public String RecibidoPor { get; set; } 



    }






}
