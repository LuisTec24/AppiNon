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

        public async Task ProcesarPredicciones(PinonBdContext db)
        {
            var productos = await db.Producto
                .Where(p => p.Reabastecimientoautomatico)
                .ToListAsync();

            foreach (var producto in productos)
            {
                try
                {
                    var (minimo, ideal, metodo) = await CalcularNivelesStock(producto, db);

                    var inventario = await db.Inv.FirstOrDefaultAsync(i => i.IdProducto == producto.Id_producto);
                    if (inventario == null)
                    {
                        _logger.LogWarning($"No inventory record found for product {producto.Id_producto}.");
                        continue; // Skip processing this product
                    }

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
            // 1. Determinar el método de predicción
            var metodo = producto.Metodoprediccion ?? "Automatico";
            if (metodo == "Automatico")
            {
                var historialCount = await db.Pedidos
                    .Where(p => p.IdProducto == producto.Id_producto && p.Estado == "Recibido")
                    .CountAsync();
                metodo = historialCount < 12 ? "General" :
                       await TieneEstacionalidad(producto.Id_producto, db) ? "Mensual" : "General";
            }

            // 2. Calcular consumo mensual ajustado
            double consumoMensual;
            if (metodo == "General")
            {
                var pedidos = await db.Pedidos
                    .Where(p => p.IdProducto == producto.Id_producto && p.Estado == "Recibido")
                    .OrderBy(p => p.FechaRecepcion)
                    .ToListAsync();

                if (pedidos.Count < 3)
                    return (5, 15, "Por defecto (datos insuficientes)");

                // Calcular consumo considerando el período real
                var diasPeriodo = (pedidos.Last().FechaRecepcion - pedidos.First().FechaRecepcion)?.TotalDays ?? 30;
                if (diasPeriodo < 30) diasPeriodo = 30; // Mínimo 1 mes

                var totalConsumo = pedidos.Sum(p => p.Cantidad);
                consumoMensual = (totalConsumo / diasPeriodo) * 30;
            }
            else // Método Mensual
            {
                var mesActual = DateTime.Now.Month;
                var pedidosMes = await db.Pedidos
                    .Where(p => p.IdProducto == producto.Id_producto &&
                               p.Estado == "Recibido" &&
                               p.FechaRecepcion.HasValue &&
                               p.FechaRecepcion.Value.Month == mesActual)
                    .ToListAsync();

                if (pedidosMes.Count < 3)
                {
                    var inventario = await db.Inv
                  .FirstOrDefaultAsync(i => i.IdProducto == producto.Id_producto);
                    return (
                                inventario?.StockMinimo ?? 5,
                                inventario?.StockIdeal ?? 15,
                                "Por defecto (usando valores registrados en inventario)"
                            );
                }
                consumoMensual = pedidosMes.Average(p => p.Cantidad);
            }

            // 3. Obtener parámetros
            var proveedor = await db.Proveedores.FirstOrDefaultAsync(p => p.ID_proveedor == producto.Id_provedor);
            int diasEntrega = proveedor?.Tiempo_entrega_dias ?? 5;
            double factorMinimo = 1.30; // Valor fijo según tu parámetro
            double factorIdeal = 1.80;  // Valor fijo según tu parámetro

            // 4. Cálculos mejorados
            double consumoDiario = consumoMensual / 30;

            // Stock mínimo: cubre tiempo de entrega + 7 días de seguridad
            int minimo = (int)Math.Ceiling(consumoDiario * (diasEntrega + 7) * factorMinimo);

            // Stock ideal: cubre tiempo de entrega + 1 mes de operación
            int ideal = (int)Math.Ceiling(consumoDiario * (diasEntrega + 30) * factorIdeal);

            // 5. Aplicar límites razonables
            minimo = Math.Max(minimo, 5); // Mínimo absoluto de 5 unidades
            ideal = Math.Max(ideal, minimo * 2); // Ideal debe ser al menos el doble del mínimo

            // Para productos con muy bajo consumo, establecer máximos
            if (consumoMensual < 10)
            {
                minimo = Math.Min(minimo, 10);
                ideal = Math.Min(ideal, 30);
            }

            return (minimo, ideal, metodo);
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