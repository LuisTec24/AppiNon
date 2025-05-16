using AppiNon.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AppiNon.Controllers
{
    [EnableCors("ReglasCors")]
    [Route("api/[controller]")]
    [ApiController]
     [Authorize] 
    public class ProveedoresController : ControllerBase
    {
        private readonly PinonBdContext _context;

        public ProveedoresController(PinonBdContext context)
        {
            _context = context;
        }

        [HttpGet]
        [Authorize(Roles = "1")]
        public async Task<ActionResult<IEnumerable<Proveedores>>> GetProveedores()
        {
            return await _context.Proveedores.ToListAsync();
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "1")]
        public async Task<ActionResult<Proveedores>> GetProveedores(int id)
        {
            var proveedor = await _context.Proveedores.FindAsync(id);

            if (proveedor == null)
            {
                return NotFound();
            }

            return proveedor;
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "1")]
        public async Task<IActionResult> PutProveedores(int id, Proveedores proveedor)
        {
            if (id != proveedor.ID_proveedor)
            {
                return BadRequest("El ID no coincide con el proveedor proporcionado.");
            }

            _context.Entry(proveedor).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
                await RegistrarBitacora("UPDATE", "Proveedores", proveedor.ID_proveedor, $"Se actualizó el proveedor: {proveedor.Nombre_Proveedor}");
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ProveedoresExists(id))
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
        public async Task<ActionResult<Proveedores>> PostProveedores(Proveedores proveedor)
        {
            _context.Proveedores.Add(proveedor);
            await _context.SaveChangesAsync();
            await RegistrarBitacora("INSERT", "Proveedores", proveedor.ID_proveedor, $"Se agregó el proveedor: {proveedor.Nombre_Proveedor}");

            return CreatedAtAction(nameof(GetProveedores), new { id = proveedor.ID_proveedor }, proveedor);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "1")]
        public async Task<IActionResult> DeleteProveedores(int id)
        {
            var proveedor = await _context.Proveedores.FindAsync(id);
            if (proveedor == null)
            {
                return NotFound();
            }

            _context.Proveedores.Remove(proveedor);
            await _context.SaveChangesAsync();
            await RegistrarBitacora("DELETE", "Proveedores", proveedor.ID_proveedor, $"Se eliminó el proveedor: {proveedor.Nombre_Proveedor}");

            return NoContent();
        }

        private bool ProveedoresExists(int id)
        {
            return _context.Proveedores.Any(e => e.ID_proveedor == id);
        }

        private async Task RegistrarBitacora(string tipo, string entidad, int idEntidad, string descripcion)
        {
            var bitacora = new Bitacora
            {
                Fecha = DateTime.Now,
                Tipo_de_Modificacion = tipo,
                ID_Usuario = 1, // puedes cambiarlo por el usuario logueado en el futuro
                Entidad = entidad,
                ID_Entidad = idEntidad,
                Descripcion = descripcion
            };

            _context.Bitacora.Add(bitacora);
            await _context.SaveChangesAsync();
        }
    }
}
