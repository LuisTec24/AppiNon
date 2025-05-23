using AppiNon.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Claims;

using System.Threading.Tasks;

namespace AppiNon.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PedidosController : ControllerBase
    {
        private readonly PinonBdContext _db;
        private readonly ILogger<PedidosController> _logger;

        public PedidosController(PinonBdContext db, ILogger<PedidosController> logger)
        {
            _db = db;
            _logger = logger;
        }

        /// <summary>
        /// Crea un pedido manualmente con validación de stock
        /// </summary>
        /// <param name="request">Datos para la creación del pedido</param>
        /// <returns>Información del pedido creado</returns>
        [HttpPost("CrearPedidoManual")]
        [Authorize(Roles = "1")]
        [ProducesResponseType(200, Type = typeof(object))]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> CrearPedidoManual([FromBody] CrearPedidoManualRequest request)
        {
            try
            {
                // 1. Validar el modelo
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                // 2. Verificar que el producto existe
                var producto = await _db.Producto
                    .FirstOrDefaultAsync(p => p.Id_producto == request.IdProducto);

                if (producto == null)
                    return NotFound($"Producto con ID {request.IdProducto} no encontrado");

                // 3. Obtener el inventario del producto
                var inventario = await _db.Inv
                    .FirstOrDefaultAsync(i => i.IdProducto == request.IdProducto);

                if (inventario == null)
                {
                    // Retornar un código y mensaje especial para que el frontend pueda redirigir
                    return StatusCode(409, new
                    {
                        Success = false,
                        Redirect = true,
                        Message = "No existe inventario para este producto. Redirigir a la creación de inventario."
                    });
                }

                // 5. Verificar pedidos pendientes existentes
                var tienePedidosPendientes = await _db.Pedidos
                    .AnyAsync(p => p.IdProducto == request.IdProducto &&
                                (p.Estado == "Pendiente" || p.Estado == "Enviado"));

                if (tienePedidosPendientes)
                    return BadRequest("Ya existe un pedido pendiente o enviado para este producto");

                // 6. Obtener el proveedor
                var proveedor = await _db.Proveedores
                    .FirstOrDefaultAsync(p => p.ID_proveedor == producto.Id_provedor);

                if (proveedor == null)
                    return BadRequest("No se encontró el proveedor asociado al producto");

                // 7. Crear el pedido
                var pedido = new Pedido
                {
                    IdProducto = request.IdProducto,
                    Cantidad = request.Cantidad,
                    Estado = "Pendiente",
                    IdProveedor = proveedor.ID_proveedor,
                    FechaSolicitud = DateTime.Now,
                    FechaRecepcion = null,
                    SolicitadoPor = User.Identity?.Name ?? "Desconocido",
                };

                _db.Pedidos.Add(pedido);
                await _db.SaveChangesAsync();

                // 8. Registrar en bitácora
                await RegistrarBitacora("Creación", "Pedido", pedido.IdPedido,
                    $"Pedido manual creado para producto {producto.Nombre_producto}");

                // 9. Retornar respuesta
                return Ok(new
                {
                    Success = true,
                    PedidoId = pedido.IdPedido,
                    Producto = producto.Nombre_producto,
                    Cantidad = pedido.Cantidad,
                    Proveedor = proveedor.Nombre_Proveedor,
                    Estado = pedido.Estado,
                    FechaSolicitud = pedido.FechaSolicitud,
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear pedido manual");
                return StatusCode(500, new { Success = false, Message = "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Registra una acción en la bitácora del sistema
        /// </summary>
        private async Task RegistrarBitacora(string tipo, string entidad, int idEntidad, string descripcion)
        {
            try
            {
                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                int userId = string.IsNullOrEmpty(userIdClaim) ? 1 : int.Parse(userIdClaim);

                var bitacora = new Bitacora
                {
                    Fecha = DateTime.Now,
                    Tipo_de_Modificacion = tipo,
                    ID_Usuario = userId,
                    Entidad = entidad,
                    ID_Entidad = idEntidad,
                    Descripcion = descripcion
                };

                _db.Bitacora.Add(bitacora);
                await _db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al registrar en bitácora");
                // No lanzamos excepción para no interrumpir el flujo principal
            }
        }
    }

    /// <summary>
    /// DTO para la creación manual de pedidos
    /// </summary>
    public class CrearPedidoManualRequest
    {
        [Required(ErrorMessage = "El ID del producto es requerido")]
        [Range(1, int.MaxValue, ErrorMessage = "El ID de producto debe ser válido")]
        public int IdProducto { get; set; }

        [Required(ErrorMessage = "La cantidad es requerida")]
        [Range(1, int.MaxValue, ErrorMessage = "La cantidad debe ser mayor que 0")]
        public int Cantidad { get; set; }

    }




}