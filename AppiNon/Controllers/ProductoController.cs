using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
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

        // GET: api/ProductoController
        [HttpGet]
        [Authorize(Roles = "1")]
        public async Task<ActionResult<IEnumerable<Producto>>> GetProducto()
        {
            return await _context.Producto.ToListAsync();
        }

        // GET: api/ProductoController/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Producto>> GetProducto(int id)
        {
            var Producto = await _context.Producto.FindAsync(id);

            if (Producto == null)
            {
                return NotFound();
            }

            return Producto;
        }

        // PUT: api/ProductoController/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        [Authorize(Roles = "1")] //
        public async Task<IActionResult> PutProducto(int id, Producto Producto)
        {
            if (id != Producto.id_producto)
            {
                return BadRequest();
            }

            _context.Entry(Producto).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ProductoExists(id))
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


        // POST: api/ProductoController
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        [Authorize(Roles = "1")] //
        public async Task<ActionResult<Producto>> PostProducto(Producto Producto)
        {
            _context.Producto.Add(Producto);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetProducto", new { id = Producto.id_producto }, Producto);
        }

        // DELETE: api/ProductoController/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "1")] //
        public async Task<IActionResult> DeleteProducto(int id)
        {
            var Producto = await _context.Producto.FindAsync(id);
            if (Producto == null)
            {
                return NotFound();
            }

            _context.Producto.Remove(Producto);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool ProductoExists(int id)
        {
            return _context.Producto.Any(e => e.id_producto == id);
        }
    }
}
