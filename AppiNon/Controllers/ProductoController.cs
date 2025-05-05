using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using AppiNon.Models;

namespace AppiNon.Controllers 
{
    [EnableCors("ReglasCors")]
    [Route("api/[controller]")]
    [Authorize]

    [ApiController]

    public class ProductoController : ControllerBase
    {
        private readonly PinonBdContext _context;
        public ProductoController(PinonBdContext context)
        {
            _context = context;
        }

        [HttpGet]
        [Authorize(Roles = "1,2")]
        public async Task<ActionResult<IEnumerable<Producto>>> GetProducto()
        {
            return await _context.Producto.ToListAsync();
        }

        [HttpGet("{id_categoria:int}/{id_producto:int}")]
        [Authorize(Roles = "1,2")]
        public async Task<ActionResult<Producto>> GetProducto(int id_categoria, int id_producto)
        {
            var producto = await _context.Producto.FindAsync(id_categoria, id_producto);

            if (producto == null)
            {
                return NotFound();
            }
            return producto;
        }

        [HttpPut("{id_categoria:int}/{id_producto:int}")]
        [Authorize(Roles = "1")]
        public async Task<IActionResult> PutProducto(int id_categoria, int id_producto, Producto producto)
        {
            if (id_categoria != producto.id_categoria || id_producto != producto.id_producto)
            {
                return BadRequest("Los identificadores no coinciden con el producto enviado.");
            }

            _context.Entry(producto).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
                await RegistrarBitacora("UPDATE", "Producto", producto.id_producto, $"Se actualizo el producto: {producto.nombre_producto}");
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ProductoExists(id_categoria, id_producto))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        [HttpPost]
        [Authorize(Roles = "1")]
        public async Task<ActionResult<Producto>> PostProducto(Producto producto)
        {
            _context.Producto.Add(producto);
            await _context.SaveChangesAsync();
            await RegistrarBitacora("INSERT", "Producto", producto.id_producto, $"Se agrego el producto: {producto.nombre_producto}");

            return CreatedAtAction(nameof(GetProducto), new
            {
                id_categoria = producto.id_categoria,
                id_producto = producto.id_producto
            }, producto);
        }

        [HttpDelete("{id_categoria:int}/{id_producto:int}")]
        [Authorize(Roles = "1")]
        public async Task<IActionResult> DeleteProducto(int id_categoria, int id_producto)
        {
            var producto = await _context.Producto.FindAsync(id_categoria, id_producto);
            if (producto == null)
            {
                return NotFound();
            }

            _context.Producto.Remove(producto);
            await _context.SaveChangesAsync();
            await RegistrarBitacora("DELETE", "Producto", producto.id_producto, $"Se elimino el producto: {producto.nombre_producto}");

            return NoContent();
        }

        private bool ProductoExists(int id_categoria, int id_producto)
        {
            return _context.Producto.Any(e => e.id_categoria == id_categoria && e.id_producto == id_producto);
        }

        private async Task RegistrarBitacora(string tipo, string entidad, int idEntidad, string descripcion)
        {
            var bitacora = new Bitacora
            {
                Fecha = DateTime.Now,
                Tipo_de_Modificacion = tipo,
                ID_Usuario = 1, // aqui puedes cambiarlo por el ID del usuario logueado
                Entidad = entidad,
                ID_Entidad = idEntidad,
                Descripcion = descripcion
            };

            _context.Bitacora.Add(bitacora);
            await _context.SaveChangesAsync();
        }
    }
}
