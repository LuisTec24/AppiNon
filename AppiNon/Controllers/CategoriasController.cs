using System;
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
    public class CategoriasController : ControllerBase
    {
        private readonly PinonBdContext _context;

        public CategoriasController(PinonBdContext context)
        {
            _context = context;
        }

        [HttpGet]
        [Authorize(Policy = "Admin")]
        [Authorize(Policy = "User")]
        public async Task<ActionResult<IEnumerable<Categorias>>> GetCategorias()
        {
            return await _context.Categorias.ToListAsync();
        }

        [HttpGet("{id:int}")]
        [Authorize(Policy = "Admin")]
        [Authorize(Policy = "User")]
        public async Task<ActionResult<Categorias>> GetCategoria(int id)
        {
            var categoria = await _context.Categorias.FindAsync(id);

            if (categoria == null)
            {
                return NotFound();
            }

            return categoria;
        }

        [HttpPut("{id:int}")]
        [Authorize(Policy = "Admin")]
        public async Task<IActionResult> PutCategoria(int id, Categorias categoria)
        {
            if (id != categoria.id_categoria)
            {
                return BadRequest("El ID no coincide con la categoría proporcionada.");
            }

            _context.Entry(categoria).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
                await RegistrarBitacora("UPDATE", "Categorias", categoria.id_categoria, $"Se actualizó la categoría: {categoria.Categoria}");
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CategoriaExists(id))
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
        [Authorize(Policy = "Admin")]
        public async Task<ActionResult<Categorias>> PostCategoria(Categorias categoria)
        {
            _context.Categorias.Add(categoria);
            await _context.SaveChangesAsync();
            await RegistrarBitacora("INSERT", "Categorias", categoria.id_categoria, $"Se agregó la categoría: {categoria.Categoria}");

            return CreatedAtAction(nameof(GetCategoria), new { id = categoria.id_categoria }, categoria);
        }

        [HttpDelete("{id:int}")]
        [Authorize(Policy = "Admin")]
        public async Task<IActionResult> DeleteCategoria(int id)
        {
            var categoria = await _context.Categorias.FindAsync(id);
            if (categoria == null)
            {
                return NotFound();
            }

            _context.Categorias.Remove(categoria);
            await _context.SaveChangesAsync();
            await RegistrarBitacora("DELETE", "Categorias", id, $"Se eliminó la categoría: {categoria.Categoria}");

            return NoContent();
        }

        private bool CategoriaExists(int id)
        {
            return _context.Categorias.Any(e => e.id_categoria == id);
        }

        private async Task RegistrarBitacora(string tipo, string entidad, int idEntidad, string descripcion)
        {
            var bitacora = new Bitacora
            {
                Fecha = DateTime.Now,
                Tipo_de_Modificacion = tipo,
                ID_Usuario = 1, // ⚠️ Reemplazar con ID del usuario logueado si tienes autenticación implementada
                Entidad = entidad,
                ID_Entidad = idEntidad,
                Descripcion = descripcion
            };

            _context.Bitacora.Add(bitacora);
            await _context.SaveChangesAsync();
        }
    }
}
