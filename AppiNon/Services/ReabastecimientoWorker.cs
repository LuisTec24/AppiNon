using AppiNon.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;


namespace AppiNon.Services
{
    public class ReabastecimientoWorker : BackgroundService
    {
        private readonly ILogger<ReabastecimientoWorker> _logger;
        private readonly IServiceProvider _services;
        private readonly TimeSpan _intervalo = TimeSpan.FromHours(24); // Ejecutar diariamente
        private readonly TimeSpan _horaEjecucion = new TimeSpan(2, 0, 0); // 2:00 AM

        public ReabastecimientoWorker(
            ILogger<ReabastecimientoWorker> logger,
            IServiceProvider services)
        {
            _logger = logger;
            _services = services;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Servicio de Reabastecimiento Automático iniciado.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var ahora = DateTime.Now;
                    var siguienteEjecucion = ahora.Date.Add(_horaEjecucion);

                    // Si ya pasó la hora de hoy, programar para mañana
                    if (ahora > siguienteEjecucion)
                    {
                        siguienteEjecucion = siguienteEjecucion.AddDays(1);
                    }

                    var tiempoEspera = siguienteEjecucion - ahora;

                    _logger.LogInformation($"Próxima ejecución programada para: {siguienteEjecucion}");

                    await Task.Delay(tiempoEspera, stoppingToken);

                    using (var scope = _services.CreateScope())
                    {
                        var context = scope.ServiceProvider.GetRequiredService<PinonBdContext>();

                        // Obtener todos los productos con inventario
                        var productosConInventario = await context.Producto
                            .Join(context.Inv,
                                p => p.id_producto,
                                i => i.IdProducto,
                                (p, i) => new { Producto = p, Inventario = i })
                            .ToListAsync();

                        _logger.LogInformation($"Verificando {productosConInventario.Count} productos...");

                        int pedidosGenerados = 0;

                        foreach (var item in productosConInventario)
                        {
                            try
                            {
                                // Obtener proveedor
                                var proveedor = await context.Proveedores
                                    .FirstOrDefaultAsync(p => p.ID_proveedor == item.Producto.ID_Provedor);

                                if (proveedor == null)
                                {
                                    _logger.LogWarning($"No se encontró proveedor para producto ID {item.Producto.id_producto}");
                                    continue;
                                }

                                // Calcular demanda promedio
                                var demanda = await context.ConsumosMensuales
                                    .Where(c => c.id_producto == item.Producto.id_producto)
                                    .OrderByDescending(c => c.Mes)
                                    .Take(3)
                                    .AverageAsync(c => c.Cantidad);

                                var consumoDiario = demanda / 30;
                                var diasHastaAgotarse = (int)(item.Inventario.StockActual / consumoDiario);

                                // Verificar si necesita reabastecimiento
                                if (item.Inventario.StockActual < item.Inventario.StockMinimo ||
                                    diasHastaAgotarse < proveedor.Tiempo_entrega_dias)
                                {
                                    int cantidad = Math.Max(0, item.Inventario.StockIdeal - item.Inventario.StockActual);

                                    if (cantidad > 0)
                                    {
                                        // Crear pedido
                                        var pedido = new PedidoReabastecimiento
                                        {
                                            IdProducto = item.Producto.id_producto,
                                            Cantidad = cantidad,
                                            FechaSolicitud = DateTime.Now,
                                            Estado = "Pendiente"
                                        };

                                        context.PedidosReabastecimiento.Add(pedido);
                                        pedidosGenerados++;

                                        // Autoajustar niveles si hay suficiente historial
                                        var historialCount = await context.ConsumosMensuales
                                            .CountAsync(c => c.id_producto == item.Producto.id_producto);

                                        if (historialCount >= 3)
                                        {
                                            item.Inventario.StockMinimo = (int)(demanda * proveedor.Tiempo_entrega_dias / 30 * 1.2);
                                            item.Inventario.StockIdeal = (int)(item.Inventario.StockMinimo * 1.5);
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, $"Error procesando producto ID {item.Producto.id_producto}");
                            }
                        }

                        if (pedidosGenerados > 0)
                        {
                            await context.SaveChangesAsync();
                            _logger.LogInformation($"Se generaron {pedidosGenerados} pedidos de reabastecimiento.");
                        }
                        else
                        {
                            _logger.LogInformation("No se requirieron pedidos de reabastecimiento.");
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Servicio de Reabastecimiento Automático detenido.");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error en el Servicio de Reabastecimiento Automático");
                    // Esperar antes de reintentar
                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                }
            }
        }
    }
}