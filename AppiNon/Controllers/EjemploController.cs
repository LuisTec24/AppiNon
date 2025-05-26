using AppiNon.Models;
using AppiNon.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AppiNon.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EjemploController : ControllerBase
    {
        private readonly IServiceProvider _services;
        private readonly ILogger<EjemploController> _logger;

        public EjemploController(IServiceProvider services, ILogger<EjemploController> logger)
        {
            _services = services;
            _logger = logger;
        }

        /// <summary>
        /// Ejecuta manualmente el cálculo de predicciones de stock
        /// </summary>
        [HttpPost("run-prediction")]
        
        public async Task<IActionResult> RunPrediction()
        {
            try
            {
                using var scope = _services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<PinonBdContext>();

                var predictionService = new StockPredictionService(
                    _services,
                    scope.ServiceProvider.GetRequiredService<ILogger<StockPredictionService>>()
                );

                await predictionService.ProcesarPredicciones(db);

                return Ok(new
                {
                    Success = true,
                    Message = "Predicciones de stock calculadas exitosamente",
                    Timestamp = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en ejecución manual de predicciones");
                return StatusCode(500, new
                {
                    Success = false,
                    Message = "Error al calcular predicciones",
                    Error = ex.Message
                });
            }
        }

        /// <summary>
        /// Ejecuta manualmente el proceso de reabastecimiento automático
        /// </summary>
        [HttpPost("run-replenishment")]
       
        public async Task<IActionResult> RunReplenishment()
        {
            try
            {
                using var scope = _services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<PinonBdContext>();

                var worker = new ReabastecimientoWorker(
                    scope.ServiceProvider.GetRequiredService<ILogger<ReabastecimientoWorker>>(),
                    _services
                );

                // Creamos un token de cancelación para esta ejecución manual
                var cancellationTokenSource = new CancellationTokenSource();

                // Ejecutamos el worker manualmente
                await worker.StartAsync(cancellationTokenSource.Token);

                // Obtenemos estadísticas de los pedidos generados
                var pedidosGenerados = await db.Pedidos
                    .Where(p => p.FechaSolicitud.Date == DateTime.Today && p.SolicitadoPor == "Servidor")
                    .CountAsync();

                return Ok(new
                {
                    Success = true,
                    Message = $"Proceso de reabastecimiento completado. Pedidos generados: {pedidosGenerados}",
                    Timestamp = DateTime.Now,
                    PedidosGenerados = pedidosGenerados
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en ejecución manual de reabastecimiento");
                return StatusCode(500, new
                {
                    Success = false,
                    Message = "Error al ejecutar reabastecimiento",
                    Error = ex.Message
                });
            }
        }

        /// <summary>
        /// Ejecuta ambos procesos (predicción + reabastecimiento) en secuencia
        /// </summary>
        [HttpPost("run-full-process")]
       
        public async Task<IActionResult> RunFullProcess()
        {
            try
            {
                // Ejecutar predicciones primero
                var predictionResult = await RunPrediction();
                if (((ObjectResult)predictionResult).StatusCode != 200)
                {
                    return predictionResult;
                }

                // Esperar 5 segundos para asegurar que los datos se actualicen
                await Task.Delay(5000);

                // Luego ejecutar reabastecimiento
                var replenishmentResult = await RunReplenishment();
                return replenishmentResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en proceso completo de stock");
                return StatusCode(500, new
                {
                    Success = false,
                    Message = "Error en el proceso completo",
                    Error = ex.Message
                });
            }
        }
    }
}