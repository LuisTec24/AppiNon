using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using AppiNon.Models;
using System.Security.Claims;
using System.ComponentModel.DataAnnotations.Schema;
using NuGet.Protocol;
using static AppiNon.Controllers.InventarioController;
using System.ComponentModel.DataAnnotations;

namespace AppiNon.Controllers
{
    [EnableCors("ReglasCors")]
    [Route("api/[controller]")]
    [Authorize]
    [ApiController]


    public class InventarioController : ControllerBase
    {
        private readonly PinonBdContext _context;
        private readonly ILogger<InventarioController> _logger;

        public InventarioController(PinonBdContext context, ILogger<InventarioController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/Inventario
        [HttpGet]
        [Authorize(Roles = "1")]
        public async Task<ActionResult<IEnumerable<Inventario>>> GetInventario()
        {
            try
            {
                return await _context.Inv.ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener todo el inventario");
                return StatusCode(500, new { message = "Error interno del servidor", details = ex.Message });
            }
        }

        // GET: api/Inventario/5
        [HttpGet("{id:int}")]
        [Authorize(Roles = "1")]

        public async Task<ActionResult<Inventario>> GetInventarioById(int id)
        {
            try
            {
                var inventario = await _context.Inv.FindAsync(id);

                if (inventario == null)
                {
                    return NotFound(new { message = $"No se encontró inventario con ID {id}" });
                }

                return Ok(inventario);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al obtener inventario con ID {id}");
                return StatusCode(500, new { message = "Error interno del servidor", details = ex.Message });
            }
        }

        // GET: api/Inventario/Producto/5
        [HttpGet("Producto/{productoId:int}")]
        [Authorize(Roles = "1")]

        public async Task<ActionResult<Inventario>> GetInventarioByProductoId(int productoId)
        {
            try
            {
                var inventario = await _context.Inv
                    .FirstOrDefaultAsync(i => i.IdProducto == productoId);

                if (inventario == null)
                {
                    return NotFound(new { message = $"No se encontró inventario para el producto con ID {productoId}" });
                }

                return Ok(inventario);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al obtener inventario para producto ID {productoId}");
                return StatusCode(500, new { message = "Error interno del servidor", details = ex.Message });
            }
        }

        // POST: api/Inventario
        [HttpPost("CrearInv")]
        [Authorize(Roles = "1")]
        public async Task<ActionResult<Inventario>> CreateInventario([FromBody] CrearInventarioDto inventarioDto)
        {
            try
            {
                // Validar que el producto exista
                var producto = await _context.Producto
                    .AsNoTracking() // Importante para evitar problemas de seguimiento
                    .FirstOrDefaultAsync(p => p.Id_producto == inventarioDto.IdProducto);

                if (producto == null)
                {
                    return BadRequest(new { message = $"El producto con ID {inventarioDto.IdProducto} no existe" });
                }

                // Validar que no exista ya un registro para este producto
                var inventarioExists = await _context.Inv.AnyAsync(i => i.IdProducto == inventarioDto.IdProducto);
                if (inventarioExists)
                {
                    return BadRequest(new { message = $"Ya existe un registro de inventario para el producto con ID {inventarioDto.IdProducto}" });
                }

                // Crear la entidad SIN asignar el Producto completo
                var inventario = new Inventario
                {
                    IdProducto = inventarioDto.IdProducto, // Solo asignar el ID
                    StockActual = inventarioDto.StockActual,
                    StockMinimo = inventarioDto.StockMinimo,
                    StockIdeal = inventarioDto.StockIdeal,
                    UltimaEntrada = DateTime.Now
                    // NO asignar Producto = producto aquí
                };

                _context.Inv.Add(inventario);
                await _context.SaveChangesAsync();

                await RegistrarBitacora("CREACIÓN", "INVENTARIO", inventario.IdInventario,
                    $"Nuevo registro creado para producto ID {inventario.IdProducto}");

                return CreatedAtAction(nameof(GetInventarioById), new { id = inventario.IdInventario }, inventario);
            }
            catch (DbUpdateException dbEx)
            {
                // Capturar el error interno específico
                string errorDetail = dbEx.InnerException?.Message ?? dbEx.Message;

                // Loggear el error completo
                _logger.LogError(dbEx, "Error de base de datos al crear inventario. Detalle: {ErrorDetail}", errorDetail);

                return StatusCode(500, new
                {
                    message = "Error de base de datos al guardar",
                    details = errorDetail,
                    entityState = dbEx.Entries?.Select(e => new {
                        Entity = e.Entity.GetType().Name,
                        State = e.State.ToString()
                    })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al crear inventario");
                return StatusCode(500, new
                {
                    message = "Error interno al crear inventario",
                    details = ex.Message
                });
            }
        }

        // PUT: api/Inventario/5
        [HttpPut("{id:int}")]
        [Authorize(Roles = "1")]

        public async Task<IActionResult> UpdateInventario(int id, [FromBody] Inventario inventario)
        {
            try
            {
                if (id != inventario.IdInventario)
                {
                    return BadRequest(new { message = "El ID del inventario no coincide con el ID en la URL" });
                }

                var existingInventario = await _context.Inv.FindAsync(id);
                if (existingInventario == null)
                {
                    return NotFound(new { message = $"No se encontró inventario con ID {id}" });
                }

                // Guardar valores antiguos para el log
                var oldStock = existingInventario.StockActual;
                var oldMin = existingInventario.StockMinimo;
                var oldIdeal = existingInventario.StockIdeal;

                // Actualizar propiedades
                existingInventario.StockActual = inventario.StockActual;
                existingInventario.StockMinimo = inventario.StockMinimo;
                existingInventario.StockIdeal = inventario.StockIdeal;
                existingInventario.UltimaEntrada = DateTime.Now;

                await _context.SaveChangesAsync();

                await RegistrarBitacora("ACTUALIZACIÓN", "INVENTARIO", id,
                    $"Inventario actualizado. Stock: {oldStock}→{inventario.StockActual}, " +
                    $"Mínimo: {oldMin}→{inventario.StockMinimo}, Ideal: {oldIdeal}→{inventario.StockIdeal}");

                return NoContent();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                if (!InventarioExists(id))
                {
                    return NotFound();
                }
                else
                {
                    _logger.LogError(ex, $"Error de concurrencia al actualizar inventario ID {id}");
                    return StatusCode(500, new { message = "Error de concurrencia al actualizar", details = ex.Message });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al actualizar inventario ID {id}");
                return StatusCode(500, new { message = "Error interno al actualizar", details = ex.Message });
            }
        }

        // DELETE: api/Inventario/5
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "1")]
        public async Task<IActionResult> DeleteInventario(int id)
        {
            try
            {
                var inventario = await _context.Inv.FindAsync(id);
                if (inventario == null)
                {
                    return NotFound(new { message = $"No se encontró inventario con ID {id}" });
                }

                _context.Inv.Remove(inventario);
                await _context.SaveChangesAsync();

                await RegistrarBitacora("ELIMINACIÓN", "INVENTARIO", id,
                    $"Inventario eliminado para producto ID {inventario.IdProducto}");

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al eliminar inventario ID {id}");
                return StatusCode(500, new { message = "Error interno al eliminar", details = ex.Message });
            }
        }


        //  #region Métodos Privados

        private bool InventarioExists(int id)
        {
            return _context.Inv.Any(e => e.IdInventario == id);
        }

        private async Task RegistrarBitacora(string accion, string entidad, int idEntidad, string descripcion)
        {
            try
            {
                var bitacora = new Bitacora
                {
                    Fecha = DateTime.Now,
                    Tipo_de_Modificacion = accion,
                    ID_Usuario = ObtenerUsuarioId(),
                    Entidad = entidad,
                    ID_Entidad = idEntidad,
                    Descripcion = descripcion
                };

                _context.Bitacora.Add(bitacora);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al registrar en bitácora");
            }
        }

        private int ObtenerUsuarioId()
        {
            // Implementación básica - ajusta según tu sistema de autenticación
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdClaim, out int userId))
            {
                return userId;
            }
            return 1; // Default si no se puede obtener
        }
        //// RegistrosPedidos//////////////////////////////////////////////////////////////////////////////////////////
        ///




        // Método auxiliar para bitácora

        /////////////////////////////////////////////////Actualizar Pedido
        ///

        [HttpPut("ActualizarEstadoPedido/{idPedido}")]
        [Authorize(Roles = "1")]
        public async Task<IActionResult> ActualizarEstadoPedido(int idPedido, [FromBody] string nuevoEstado)
        {
            try
            {
                var pedido = await _context.Pedidos.FindAsync(idPedido);
                if (pedido == null) return NotFound("Pedido no encontrado");

                // Validar estado
                if (!new[] { "Pendiente", "Enviado", "Recibido", "Cancelado" }.Contains(nuevoEstado))
                    return BadRequest("Estado no válido");

                // Si se recibe, actualizar inventario
                if (nuevoEstado == "Recibido")
                {
                    pedido.FechaRecepcion = DateTime.Now;

                    var inventario = await _context.Inv.FirstOrDefaultAsync(i => i.IdProducto == pedido.IdProducto);
                    if (inventario != null)
                    {
                        inventario.StockActual += pedido.Cantidad;
                        inventario.UltimaEntrada = DateTime.Now;
                    }
                }

                pedido.Estado = nuevoEstado;
                await _context.SaveChangesAsync();

                return Ok(pedido);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar estado del pedido");
                return StatusCode(500, "Error interno");
            }
        }




        ////--------------------Consultar Pedidos
        ///

        [HttpGet("PedidosPendientes")]
        [Authorize(Roles = "1,2")]
        public async Task<IActionResult> GetPedidosPendientes()
        {
            var pedidos = await _context.Pedidos
                .Include(p => p.Producto)
                .Include(p => p.Proveedor)
                .Select(p => new
                {
                    p.IdPedido,
                    p.Cantidad,
                    FechaSolicitud = p.FechaSolicitud,
                     p.SolicitadoPor,
                    FechaRecepcion = p.FechaRecepcion.HasValue ? p.FechaRecepcion.Value.ToString("yyyy-MM-dd") : "Pendiente",
                    ProductoNombre = p.Producto != null ? p.Producto.Nombre_producto : "No asignado",
                    ProveedorNombre = p.Proveedor != null ? p.Proveedor.Nombre_Proveedor : "No asignado",
                    RecibidoPor = !string.IsNullOrEmpty(p.RecibidoPor) ? p.RecibidoPor : "No recibido",
                    Estado = !string.IsNullOrEmpty(p.Estado) ? p.Estado : "Pendiente"
                })
                .ToListAsync();

            return Ok(pedidos);
        }
    }
    }

