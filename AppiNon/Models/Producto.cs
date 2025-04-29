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
        public string unidad_medida { get; set; } = null!;

    }
}
