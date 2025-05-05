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
            [Authorize(Roles = "1,2")]
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
            [Authorize(Roles = "1,2")]
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
            [Authorize(Roles = "1,2")]
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
            [HttpPost]
            [Authorize(Roles = "1")]
            public async Task<ActionResult<Inventario>> CreateInventario([FromBody] Inventario inventario)
            {
                try
                {
                    // Validar que el producto exista
                    var productoExists = await _context.Producto.AnyAsync(p => p.id_producto == inventario.IdProducto);
                    if (!productoExists)
                    {
                        return BadRequest(new { message = $"El producto con ID {inventario.IdProducto} no existe" });
                    }

                    // Validar que no exista ya un registro para este producto
                    var inventarioExists = await _context.Inv.AnyAsync(i => i.IdProducto == inventario.IdProducto);
                    if (inventarioExists)
                    {
                        return BadRequest(new { message = $"Ya existe un registro de inventario para el producto con ID {inventario.IdProducto}" });
                    }

                    inventario.UltimaEntrada = DateTime.Now;
                    _context.Inv.Add(inventario);
                    await _context.SaveChangesAsync();

                    await RegistrarBitacora("CREACIÓN", "INVENTARIO", inventario.IdInventario,
                        $"Nuevo registro creado para producto ID {inventario.IdProducto} con stock inicial {inventario.StockActual}");

                    return CreatedAtAction(nameof(GetInventarioById), new { id = inventario.IdInventario }, inventario);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al crear nuevo registro de inventario");
                    return StatusCode(500, new { message = "Error interno al crear inventario", details = ex.Message });
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

            // PATCH: api/Inventario/AjustarStock/5
            [HttpPatch("AjustarStock/{id:int}")]
            [Authorize(Roles = "1")]
            public async Task<IActionResult> AdjustStock(int id, [FromBody] int cantidad)
            {
                try
                {
                    var inventario = await _context.Inv.FindAsync(id);
                    if (inventario == null)
                    {
                        return NotFound(new { message = $"No se encontró inventario con ID {id}" });
                    }

                    var oldStock = inventario.StockActual;
                    inventario.StockActual += cantidad;
                    inventario.UltimaEntrada = DateTime.Now;

                    await _context.SaveChangesAsync();

                    await RegistrarBitacora("AJUSTE", "INVENTARIO", id,
                        $"Ajuste de stock: {oldStock} → {inventario.StockActual} (Cambio: {(cantidad >= 0 ? "+" : "")}{cantidad})");

                    // Verificar si necesita reabastecimiento
                    await VerificarYGenerarPedido(inventario.IdProducto);

                    return NoContent();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error al ajustar stock para inventario ID {id}");
                    return StatusCode(500, new { message = "Error interno al ajustar stock", details = ex.Message });
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

            // GET: api/Inventario/EstadoReabastecimiento/5
            [HttpGet("EstadoReabastecimiento/{productoId:int}")]
            [Authorize(Roles = "1,2")]
            public async Task<ActionResult<object>> GetReabastecimientoStatus(int productoId)
            {
                try
                {
                    // Obtener datos del producto y proveedor
                    var query = from p in _context.Producto
                                join prov in _context.Proveedores on p.ID_Provedor equals prov.ID_proveedor
                                where p.id_producto == productoId
                                select new { Producto = p, Proveedor = prov };

                    var result = await query.FirstOrDefaultAsync();

                    if (result == null)
                    {
                        return NotFound(new { message = $"No se encontró producto con ID {productoId}" });
                    }

                    var inventario = await _context.Inv.FirstOrDefaultAsync(i => i.IdProducto == productoId);
                    if (inventario == null)
                    {
                        return NotFound(new { message = $"No se encontró inventario para el producto con ID {productoId}" });
                    }

                    // Calcular demanda promedio
                    var demanda = await CalcularDemandaPromedio(productoId);
                    var consumoDiario = demanda / 30;
                    var diasHastaAgotarse = (int)(inventario.StockActual / consumoDiario);

                    // Determinar si necesita reabastecimiento
                    var necesitaReabastecer = inventario.StockActual < inventario.StockMinimo ||
                                            diasHastaAgotarse < result.Proveedor.Tiempo_entrega_dias;
                    var cantidadRecomendada = necesitaReabastecer ?
                        Math.Max(0, inventario.StockIdeal - inventario.StockActual) : 0;

                    return Ok(new
                    {
                        Producto = result.Producto.nombre_producto,
                        StockActual = inventario.StockActual,
                        StockMinimo = inventario.StockMinimo,
                        StockIdeal = inventario.StockIdeal,
                        DemandaPromedio = demanda,
                        DiasHastaAgotarse = diasHastaAgotarse,
                        TiempoEntregaProveedor = result.Proveedor.Tiempo_entrega_dias,
                        NecesitaReabastecimiento = necesitaReabastecer,
                        CantidadRecomendada = cantidadRecomendada,
                        Proveedor = result.Proveedor.Nombre_Proveedor
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error al obtener estado de reabastecimiento para producto ID {productoId}");
                    return StatusCode(500, new { message = "Error interno al calcular reabastecimiento", details = ex.Message });
                }
            }

            // POST: api/Inventario/GenerarPedido/5
            [HttpPost("GenerarPedido/{productoId:int}")]
            [Authorize(Roles = "1")]
            public async Task<IActionResult> GeneratePurchaseOrder(int productoId)
            {
                try
                {
                    var pedidoGenerado = await VerificarYGenerarPedido(productoId, true);

                    if (pedidoGenerado)
                    {
                        return Ok(new { message = "Pedido de reabastecimiento generado exitosamente" });
                    }
                    else
                    {
                        return Ok(new { message = "No se requiere reabastecimiento en este momento" });
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error al generar pedido para producto ID {productoId}");
                    return StatusCode(500, new { message = "Error interno al generar pedido", details = ex.Message });
                }
            }

            #region Métodos Privados

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

            private async Task<double> CalcularDemandaPromedio(int productoId)
            {
                try
                {
                    // Obtener consumo de los últimos 3 meses
                    var consumos = await _context.ConsumosMensuales
                        .Where(c => c.id_producto == productoId)
                        .OrderByDescending(c => c.Mes)
                        .Take(3)
                        .Select(c => c.Cantidad)
                        .ToListAsync();

                    return consumos.Any() ? consumos.Average() : 10; // Valor por defecto si no hay historial
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error al calcular demanda para producto ID {productoId}");
                    return 10; // Valor por defecto en caso de error
                }
            }

            private async Task<bool> VerificarYGenerarPedido(int productoId, bool forzar = false)
            {
                try
                {
                    // Obtener datos necesarios
                    var query = from p in _context.Producto
                                join prov in _context.Proveedores on p.ID_Provedor equals prov.ID_proveedor
                                where p.id_producto == productoId
                                select new { Producto = p, Proveedor = prov };

                    var result = await query.FirstOrDefaultAsync();

                    if (result == null)
                    {
                        _logger.LogWarning($"No se encontró producto o proveedor para ID {productoId}");
                        return false;
                    }

                    var inventario = await _context.Inv.FirstOrDefaultAsync(i => i.IdProducto == productoId);
                    if (inventario == null)
                    {
                        _logger.LogWarning($"No se encontró inventario para producto ID {productoId}");
                        return false;
                    }

                    // Calcular demanda y días hasta agotarse
                    var demanda = await CalcularDemandaPromedio(productoId);
                    var diasHastaAgotarse = (int)(inventario.StockActual / (demanda / 30));

                    // Verificar si necesita reabastecimiento
                    if (forzar || inventario.StockActual < inventario.StockMinimo ||
                        diasHastaAgotarse < result.Proveedor.Tiempo_entrega_dias)
                    {
                        int cantidad = Math.Max(0, inventario.StockIdeal - inventario.StockActual);

                        if (cantidad > 0)
                        {
                            // Crear pedido de reabastecimiento
                            var pedido = new PedidoReabastecimiento
                            {
                                IdProducto = productoId,
                                Cantidad = cantidad,
                                FechaSolicitud = DateTime.Now,
                                Estado = "Pendiente"
                            };

                            _context.PedidosReabastecimiento.Add(pedido);
                            await _context.SaveChangesAsync();

                            await RegistrarBitacora("REABASTECIMIENTO", "INVENTARIO", inventario.IdInventario,
                                $"Pedido generado para producto {result.Producto.nombre_producto}. Cantidad: {cantidad}");

                            // Autoajustar niveles si hay suficiente historial
                            var historialCount = await _context.ConsumosMensuales
                                .CountAsync(c => c.id_producto == productoId);

                            if (historialCount >= 3)
                            {
                                var nuevoMinimo = (int)(demanda * result.Proveedor.Tiempo_entrega_dias / 30 * 1.2);
                                var nuevoIdeal = (int)(nuevoMinimo * 1.5);

                                if (inventario.StockMinimo != nuevoMinimo || inventario.StockIdeal != nuevoIdeal)
                                {
                                    inventario.StockMinimo = nuevoMinimo;
                                    inventario.StockIdeal = nuevoIdeal;
                                    await _context.SaveChangesAsync();

                                    await RegistrarBitacora("AUTO-AJUSTE", "INVENTARIO", inventario.IdInventario,
                                        $"Niveles autoajustados. Mínimo: {nuevoMinimo}, Ideal: {nuevoIdeal}");
                                }
                            }

                            return true;
                        }
                    }

                    return false;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error en VerificarYGenerarPedido para producto ID {productoId}");
                    throw;
                }
            }

            #endregion
        }

}
}