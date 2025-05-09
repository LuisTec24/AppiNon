// Supongamos que ya tienes modelos Producto, Categoria y Proveedor
// Ahora creamos los endpoints para exponerlos en tu API

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using AppiNon.Models;
using Microsoft.AspNetCore.Cors;

namespace AppiNon.Controllers
{
    [EnableCors("ReglasCors")]
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] //
    public class ModelosCompletos : ControllerBase
    {
        private readonly PinonBdContext _context;

        public ModelosCompletos(PinonBdContext context)
        {
            _context = context;
        }

        [HttpGet("Productos")]
        [Authorize(Roles = "1,2")]
        public async Task<ActionResult<IEnumerable<Producto>>> GetProductos()
        {
            return await _context.Producto.ToListAsync();
        }

        [HttpGet("Categorias")]
        [Authorize(Roles = "1,2")]
        public async Task<ActionResult<IEnumerable<Categorias>>> GetCategorias()
        {
            return await _context.Categorias.ToListAsync();
        }

        [HttpGet("Provedors")]
        [Authorize(Roles = "1,2")]
        public async Task<ActionResult<IEnumerable<Proveedores>>> GetProveedores()
        {
            return await _context.Proveedores.ToListAsync();
        }

    }

}
