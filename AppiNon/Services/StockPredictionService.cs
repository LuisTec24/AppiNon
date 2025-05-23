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
    public class StockPredictionService : BackgroundService
    {
        private readonly IServiceProvider _services;
        private readonly ILogger<StockPredictionService> _logger;

        public StockPredictionService(IServiceProvider services, ILogger<StockPredictionService> logger)
        {
            _services = services;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Servicio de Predicción de Stock iniciado.");

            while (!stoppingToken.IsCancellationRequested)//si no esta suspendido osea si es primeros del mes
            {
                var ahora = DateTime.Now;
                var primerDiaProximoMes = new DateTime(ahora.Year, ahora.Month, 1).AddMonths(1);
                var diaAntesDelNuevoMes = primerDiaProximoMes.AddDays(-1);
                var tiempoEspera = diaAntesDelNuevoMes - ahora;

                await Task.Delay(tiempoEspera, stoppingToken);//espera el dia, despues inicia

                try
                {
                    using var scope = _services.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<PinonBdContext>();
                    await ProcesarPredicciones(db);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error en el servicio de predicción");
                }
            }
        }

        private async Task ProcesarPredicciones(PinonBdContext db)
        {
            var productos = await db.Producto
                .Where(p => p.Reabastecimientoautomatico)
                .ToListAsync();

            foreach (var producto in productos)
            {
                try
                {
                    var (minimo, ideal, metodo) = await CalcularNivelesStock(producto, db);

                    var inventario = await db.Inv.FirstAsync(i => i.IdProducto == producto.Id_producto);
                    inventario.StockMinimo = minimo;
                    inventario.StockIdeal = ideal;

                    // Registrar la predicción
                    db.Predicciones.Add(new Predicciones
                    {
                        id_producto = producto.Id_producto,
                        Mes = DateTime.Now.Month,
                        Ano = DateTime.Now.Year,
                        ConsumoPredicho = (minimo / GetFactor("FACTOR_STOCK_MINIMO", db)),
                        StockMinimoCalculado = minimo,
                        StockIdealCalculado = ideal,
                        MetodoUsado = metodo
                    });

                    await db.SaveChangesAsync();

                    _logger.LogInformation($"Predicción actualizada para {producto.Nombre_producto} " +
                                            $"(Método: {metodo}) - Mínimo: {minimo}, Ideal: {ideal}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error procesando producto {producto.Id_producto}");
                }
            }
        }

        private async Task<(int minimo, int ideal, string metodo)> CalcularNivelesStock(Producto producto, PinonBdContext db)
        {
            var metodo = producto.Metodoprediccion ?? "Automatico";

            if (metodo == "Automatico")
            {
                var historialCount = await db.Pedidos
                    .Where(p => p.IdProducto == producto.Id_producto && p.Estado == "Entregado")
                    .CountAsync();

                metodo = historialCount < 12
                    ? "General"
                    : await TieneEstacionalidad(producto.Id_producto, db) ? "Mensual" : "General";
            }

            double consumo;

            if (metodo == "General")
            {
                var pedidos = db.Pedidos
                    .Where(p => p.IdProducto == producto.Id_producto && p.Estado == "Entregado");

                var count = await pedidos.CountAsync();
                if (count < 3)
                {
                    _logger.LogInformation($"Producto {producto.Id_producto} omitido: solo {count} pedidos entregados.");
                    return (0, 0, metodo);
                }

                consumo = await pedidos.AverageAsync(p => (double)p.Cantidad);
            }
            else // Método Mensual
            {
                var mesActual = DateTime.Now.Month;

                var pedidosMes = db.Pedidos
                    .Where(p => p.IdProducto == producto.Id_producto &&
                                p.Estado == "Entregado" &&
                                p.FechaRecepcion.HasValue &&
                                p.FechaRecepcion.Value.Month == mesActual);

                var count = await pedidosMes.CountAsync();
                if (count < 3)
                {
                    _logger.LogInformation($"Producto {producto.Id_producto} omitido: solo {count} pedidos entregados este mes.");
                    return (0, 0, metodo);
                }

                consumo = await pedidosMes.AverageAsync(p => (double)p.Cantidad);
            }

            return (
                (int)Math.Ceiling(consumo * (double)GetFactor("FACTOR_STOCK_MINIMO", db)),
                (int)Math.Ceiling(consumo * (double)GetFactor("FACTOR_STOCK_IDEAL", db)),
                metodo
            );
        }



        private async Task<bool> TieneEstacionalidad(int productoId, PinonBdContext db)
        {
            var umbral = (double)GetFactor("UMBRAL_VARIACION_ESTACIONAL", db); // Conversión a double

            var variacion = await db.Pedidos
                .Where(p => p.IdProducto == productoId && p.FechaRecepcion != null)
                .GroupBy(p => p.FechaRecepcion.Value.Month)
                .Select(g => new { Mes = g.Key, Promedio = g.Average(p => p.Cantidad) })
                .ToListAsync();

            if (variacion.Count < 12) return false;

            var max = variacion.Max(v => v.Promedio);
            var min = variacion.Min(v => v.Promedio);

            if (max == 0) return false; // Protección contra división por cero
            return (max - min) / max > umbral;
        }

        private decimal GetFactor(string nombre, PinonBdContext db)
        {
            return db.ParametrosSistema
                .Where(p => p.Nombre == nombre)
                .Select(p => p.Valor)
                .FirstOrDefault();
        }
    }
}