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
        [Authorize(Roles = "1,2,3")]
        public async Task<IActionResult> ActualizarEstadoPedido(int IdPedido, [FromBody] EstadoRequest request)
        {
            try
            {
                _logger.LogInformation("EstadoRequest recibido: {@Request}", request);

                var pedido = await _context.Pedidos.FindAsync(IdPedido);
                if (pedido == null)
                    return NotFound(new { success = false, message = "Pedido no encontrado" }); // <-- Ahora es JSON

                // Validar estado
                if (!new[] { "Pendiente", "Enviado", "Recibido", "Rechazado" }.Contains(request.Estado))
                    return BadRequest(new { success = false, message = "Estado no válido" }); // <-- JSON

                if (pedido.Estado == "Recibido")
                    return BadRequest(new { success = false, message = "Este pedido ya fue recibido y no puede modificarse" });

                // Lógica de actualización
                if (request.Estado == "Recibido")
                {
                    pedido.FechaRecepcion = DateTime.Now;
                    pedido.RecibidoPor = User.Identity?.Name ?? "Desconocido";

                    var inventario = await _context.Inv.FirstOrDefaultAsync(i => i.IdProducto == pedido.IdProducto);
                    if (inventario != null)
                    {
                        inventario.StockActual += pedido.Cantidad;
                        inventario.UltimaEntrada = DateTime.Now;
                    }
                }

                pedido.Estado = request.Estado;
                await _context.SaveChangesAsync();

                return Ok(new { success = true, pedido }); // <-- Estructura consistente
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar estado del pedido");
                return StatusCode(500, new { success = false, message = "Error interno", details = ex.Message });
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

        public class SalidaProductoRequest
        {
            public int IdProducto { get; set; }
            public int Cantidad { get; set; }
        }
        public class SalidaMultipleRequest
        {
            public List<SalidaProductoRequest> Salidas { get; set; }
        }

        [HttpPost("registrar-salidas")]
        [Authorize(Roles = "1,2,3")]
        public async Task<IActionResult> RegistrarSalidas([FromBody] SalidaMultipleRequest request)
        {
            if (request.Salidas == null || !request.Salidas.Any())
                return BadRequest("No se recibieron productos para registrar");

            foreach (var salida in request.Salidas)
            {
                var inventario = await _context.Inv.FirstOrDefaultAsync(i => i.IdProducto == salida.IdProducto);

                if (inventario == null)
                    return NotFound($"Inventario no encontrado para producto {salida.IdProducto}");

                if (inventario.StockActual < salida.Cantidad)
                    return BadRequest($"No hay suficiente inventario para producto {salida.IdProducto}");

                // restar del inventario
                inventario.StockActual -= salida.Cantidad;

                // registrar en bitácora si tienes
                await RegistrarBitacora("Salida", "Inventario", inventario.IdInventario,
                    $"Salida registrada de {salida.Cantidad} unidades para producto {salida.IdProducto}");
            }
            await _context.SaveChangesAsync();

            return Ok(new { Success = true, Message = "Salidas registradas correctamente" });
        }




        [HttpGet("MisPedidosProvedor")]
        [Authorize(Roles = "3")] // rol del proveedor
        public async Task<IActionResult> GetMisPedidosProvedor()
        {
            // obtener el correo del usuario desde el token
            var correo = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(correo))
                return Unauthorized("no se encontro el correo del usuario.");
             // buscar el proveedor que tenga ese correo
            var proveedor = await _context.Proveedores.FirstOrDefaultAsync(p => p.Correo == correo);

            if (proveedor == null)
                return NotFound("proveedor no encontrado con el correo actual.");

            // buscar los pedidos relacionados a ese proveedor
            var pedidos = await _context.Pedidos
                .Include(p => p.Producto)
                .Where(p => p.IdProveedor == proveedor.ID_proveedor)
                .Select(p => new
                {
                    p.IdPedido,
                    p.Cantidad,
                    FechaSolicitud = p.FechaSolicitud,
                    p.SolicitadoPor,
                    FechaRecepcion = p.FechaRecepcion.HasValue ? p.FechaRecepcion.Value.ToString("yyyy-MM-dd") : "Pendiente",
                    ProductoNombre = p.Producto != null ? p.Producto.Nombre_producto : "No asignado",
                    RecibidoPor = !string.IsNullOrEmpty(p.RecibidoPor) ? p.RecibidoPor : "No recibido",
                    Estado = !string.IsNullOrEmpty(p.Estado) ? p.Estado : "Pendiente"
                })
                .ToListAsync();

            return Ok(pedidos);
        }

    }
    }

