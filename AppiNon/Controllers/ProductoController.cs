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

        [HttpPut("/Editar/{id_producto:int}")]
        [Authorize(Roles = "1")]
        public async Task<IActionResult> PutProducto(int id_producto, ProductoUpdateDto dto)
        {
            if (id_producto != dto.Id_producto)
                return BadRequest("El ID en la URL no coincide con el ID del producto.");

            // Buscar solo por id_producto
            var productoExistente = await _context.Producto
                .FirstOrDefaultAsync(p => p.Id_producto == id_producto);

            if (productoExistente == null)
                return NotFound("Producto no encontrado.");

            // Actualizar campos (nombres corregidos para coincidir con DTO)
            productoExistente.Nombre_producto = dto.Nombre_producto;
            productoExistente.Id_categoria = dto.Id_categoria;
            productoExistente.Unidad_medida = dto.Unidad_medida;
            productoExistente.Id_provedor = dto.Id_provedor;
            productoExistente.Reabastecimientoautomatico = dto.Reabastecimientoautomatico;
            productoExistente.Metodoprediccion = dto.MetodoPrediccion;

            try
            {
                await _context.SaveChangesAsync();
                await RegistrarBitacora("UPDATE", "Producto", id_producto,
                    $"Actualizado: {productoExistente.Nombre_producto}");
                return NoContent();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                return StatusCode(500, $"Error al actualizar: {ex.Message}");
            }
        }


        [HttpPost("/Agregar/")]
        [Authorize(Roles = "1")]
        public async Task<ActionResult<ProductoCreateDto>> PostProducto(ProductoCreateDto dto)
        {
            var producto = new Producto
            {
                Nombre_producto = dto.Nombre_producto,
                Id_categoria = dto.Id_categoria,
                Unidad_medida = dto.Unidad_medida,
                Id_provedor = dto.Id_Provedor,
                Reabastecimientoautomatico = dto.Reabastecimientoautomatico,
                Metodoprediccion = dto.MetodoPrediccion
            };

            _context.Producto.Add(producto);
            await _context.SaveChangesAsync();

            await RegistrarBitacora("INSERT", "Producto", producto.Id_producto,
                $"Se agregó el producto: {producto.Nombre_producto}");

            // Puedes mapear el producto de nuevo a un DTO si quieres ocultar propiedades
            return CreatedAtAction(nameof(GetProducto), new { id_producto = producto.Id_producto }, dto);
        }


        [HttpDelete("/Eliminar/{id_producto:int}")]
        [Authorize(Roles = "1")]
        public async Task<IActionResult> DeleteProducto(int id_producto)
        {
            // Buscar solo por id_producto
            var producto = await _context.Producto
                .FirstOrDefaultAsync(p => p.Id_producto == id_producto);

            if (producto == null)
                return NotFound("Producto no encontrado");

            _context.Producto.Remove(producto);
            await _context.SaveChangesAsync();

            await RegistrarBitacora("DELETE", "Producto", id_producto,
                $"Se eliminó: {producto.Nombre_producto}");

            return NoContent();
        }

        private bool ProductoExists(int id_producto)
        {
            return _context.Producto.Any(e => e.Id_producto == id_producto);
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
