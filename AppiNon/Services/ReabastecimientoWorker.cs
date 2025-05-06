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
            _logger.LogInformation("Worker de Reabastecimiento iniciado. Verificando inventarios...");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _services.CreateScope())
                    {
                        var db = scope.ServiceProvider.GetRequiredService<PinonBdContext>();

                        // 1. Obtener productos con stock bajo
                        var productosBajoStock = await db.Producto
                            .Join(db.Inv,
                                p => p.id_producto,
                                i=> i.IdProducto, 
                                (p, i) => new { Producto = p, Inventario = i })
                            .Where(x => x.Inventario.StockActual < x.Inventario.StockMinimo)
                            .ToListAsync();

                        _logger.LogInformation($"Productos con stock bajo: {productosBajoStock.Count}");

                        // 2. Procesar cada producto
                        foreach (var item in productosBajoStock)
                        {
                            var proveedor = await db.Proveedores
                                .FirstOrDefaultAsync(p => p.ID_proveedor == item.Producto.ID_Provedor);

                            if (proveedor == null)
                            {
                                _logger.LogWarning($"Proveedor no encontrado para producto ID: {item.Producto.id_producto}");
                                continue;
                            }

                            // 3. Calcular cantidad a pedir
                            int cantidad = item.Inventario.StockIdeal - item.Inventario.StockActual;
                            if (cantidad <= 0) continue;

                            // 4. Crear pedido (si no existe uno pendiente)
                            bool pedidoExistente = await db.Pedidos
                                .AnyAsync(p => p.IdProducto == item.Producto.id_producto && p.Estado == "Pendiente");

                            if (!pedidoExistente)
                            {
                                db.Pedidos.Add(new Pedido
                                {
                                    IdProducto = item.Producto.id_producto,
                                    Cantidad = cantidad,
                                    Estado = "Pendiente",
                                    IdProveedor = item.Producto.ID_Provedor
                                });

                                await db.SaveChangesAsync();
                                _logger.LogInformation($"Pedido generado para {item.Producto.nombre_producto}");
                            }
                        }
                    }

                    // Esperar 24 horas
                    await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error en el worker. Reintentando en 30 minutos...");
                    await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken);
                }
            }
        }
    }
}