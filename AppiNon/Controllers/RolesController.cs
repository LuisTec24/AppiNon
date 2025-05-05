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
    public class RolesController : ControllerBase
    {
        private readonly PinonBdContext _context;

        public RolesController(PinonBdContext context)
        {
            _context = context;
        }

        [HttpGet]
        [Authorize(Roles = "1,2")]
        public async Task<ActionResult<IEnumerable<Roles>>> GetRoles()
        {
            return await _context.Roles.ToListAsync();
        }

        [HttpGet("{id:int}")]
        [Authorize(Roles = "1,2")]
        public async Task<ActionResult<Roles>> GetRol(int id)
        {
            var rol = await _context.Roles.FindAsync(id);

            if (rol == null)
            {
                return NotFound();
            }

            return rol;
        }

        [HttpPost]
        [Authorize(Roles = "1")]
        public async Task<ActionResult<Roles>> PostRol(Roles rol)
        {
            if (string.IsNullOrEmpty(rol.Nombre_rol))
            {
                return BadRequest("El nombre del rol es requerido");
            }

            _context.Roles.Add(rol);
            await _context.SaveChangesAsync();
            await RegistrarBitacora("INSERT", "Roles", rol.ID, $"Se agregó el rol: {rol.Nombre_rol}");

            return CreatedAtAction(nameof(GetRol), new { id = rol.ID }, rol);
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "1")]
        public async Task<IActionResult> PutRol(int id, Roles rol)
        {
            if (id != rol.ID)
            {
                return BadRequest("El ID del rol no coincide");
            }

            _context.Entry(rol).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
                await RegistrarBitacora("UPDATE", "Roles", rol.ID, $"Se actualizó el rol: {rol.Nombre_rol}");
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!RolExists(id))
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

        [HttpDelete("{id:int}")]
        [Authorize(Roles = "1")]
        public async Task<IActionResult> DeleteRol(int id)
        {
            var rol = await _context.Roles.FindAsync(id);
            if (rol == null)
            {
                return NotFound();
            }

            // Verificar si hay usuarios con este rol
            if (await _context.Usuarios.AnyAsync(u => u.Rol_id == id))
            {
                return BadRequest("No se puede eliminar el rol porque está asignado a usuarios");
            }

            _context.Roles.Remove(rol);
            await _context.SaveChangesAsync();
            await RegistrarBitacora("DELETE", "Roles", rol.ID, $"Se eliminó el rol: {rol.Nombre_rol}");

            return NoContent();
        }

        private bool RolExists(int id)
        {
            return _context.Roles.Any(e => e.ID == id);
        }

        private async Task RegistrarBitacora(string tipo, string entidad, int idEntidad, string descripcion)
        {
            var bitacora = new Bitacora
            {
                Fecha = DateTime.Now,
                Tipo_de_Modificacion = tipo,
                ID_Usuario = 1, // Aquí deberías usar el ID del usuario autenticado
                Entidad = entidad,
                ID_Entidad = idEntidad,
                Descripcion = descripcion
            };

            _context.Bitacora.Add(bitacora);
            await _context.SaveChangesAsync();
        }
    }
}