using AppiNon.Controllers;
using AppiNon.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AppiNon.Services
{
    public class ReabastecimientoWorker : BackgroundService
    {
        private readonly ILogger<ReabastecimientoWorker> _logger;
        private readonly IServiceProvider _services;

        public ReabastecimientoWorker(
            ILogger<ReabastecimientoWorker> logger,
            IServiceProvider services)
        {
            _logger = logger;
            _services = services;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Worker de Reabastecimiento iniciado.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _services.CreateScope())
                    {
                        var db = scope.ServiceProvider.GetRequiredService<PinonBdContext>();

                        // Solo productos con reabastecimiento automático
                        var productosBajoStock = await db.Producto
                            .Where(p => p.Reabastecimientoautomatico) // Solo los automáticos
                            .Join(db.Inv,
                                p => p.Id_producto,
                                i => i.IdProducto,
                                (p, i) => new { Producto = p, Inventario = i })
                            .Where(x => x.Inventario.StockActual < x.Inventario.StockMinimo)
                            .GroupJoin(db.Pedidos.Where(p => p.Estado == "Pendiente" || p.Estado == "Enviado"),
                                x => x.Producto.Id_producto,
                                p => p.IdProducto,
                                (x, pedidos) => new { x.Producto, x.Inventario, Pedidos = pedidos })
                            .Where(x => !x.Pedidos.Any())
                            .Select(x => new { x.Producto, x.Inventario })
                            .ToListAsync();

                        _logger.LogInformation($"Productos con reabastecimiento automático necesitados: {productosBajoStock.Count}");

                        foreach (var item in productosBajoStock)
                        {
                            var proveedor = await db.Proveedores
                                .FirstOrDefaultAsync(p => p.ID_proveedor == item.Producto.Id_provedor);
                            
                            if (proveedor == null)
                            {
                                _logger.LogWarning($"Proveedor no encontrado para producto ID: {item.Producto.Id_producto}");
                                continue;
                            }


                            //aqui pediria el stock ideal que deberiamos tener para el siguiente mes , se supone que antes se calculo el stock ideal
                            int cantidad = Math.Max(1, item.Inventario.StockIdeal - item.Inventario.StockActual);
                            //esto lo tengo que cambiar por pedido desde controles
                            var nuevoPedido = new Pedido
                            {
                                IdProducto = item.Producto.Id_producto,
                                Cantidad = cantidad,
                                Estado = "Pendiente",
                                IdProveedor = item.Producto.Id_provedor,
                                FechaSolicitud = DateTime.Now,
                                SolicitadoPor="Servidor"
                            };

                            db.Pedidos.Add(nuevoPedido);
                            await db.SaveChangesAsync();

                            _logger.LogInformation($"Pedido automático {nuevoPedido.IdPedido} generado para {item.Producto.Nombre_producto}");


                            var correo = new Correo();
                            string asunto = "Nuevo pedido generado";
                            string cuerpo = $"Se ha generado un pedido para el producto {item.Producto.Nombre_producto}.\n" +
                                            $"Cantidad: {nuevoPedido.Cantidad}\n" +
                                            $"Fecha: {nuevoPedido.FechaSolicitud:dd/MM/yyyy hh:mm tt}";

                            var Prueba = "lg4595422@gmail.com";//ajustar correo aqui iria el correo del provedor

                            correo.EnviarCorreo(Prueba, asunto, cuerpo);
                        }
                    }

                    await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error en el worker de reabastecimiento");
                    await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken);
                }
            }
        }

        private async Task RegistrarBitacora(PinonBdContext db, string tipo, string entidad, int idEntidad, string descripcion)
        {
            // Aquí deberías obtener el ID del usuario del sistema (no hardcodeado)
            var bitacora = new Bitacora
            {
                Fecha = DateTime.Now,
                Tipo_de_Modificacion = tipo,
                ID_Usuario = 1, // Reemplazar con usuario real
                Entidad = entidad,
                ID_Entidad = idEntidad,
                Descripcion = descripcion
            };

            db.Bitacora.Add(bitacora);
            await db.SaveChangesAsync();
        }

    }
}