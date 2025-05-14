using AppiNon.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AppiNon.Controllers
{
    using AppiNon.Models;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;
    using System;
    using System.Linq;
    using System.Threading.Tasks;

    [ApiController]
    [Route("api/stock-info")]
    public class StockQueryController : ControllerBase
    {
        private readonly PinonBdContext _db;

        public StockQueryController(PinonBdContext db)
        {
            _db = db;
        }

        [HttpGet("predicciones/{idProducto}")]
        public async Task<IActionResult> GetPredicciones(int idProducto, [FromQuery] int limit = 12)
        {
            var predicciones = await _db.Predicciones
                .Where(p => p.id_producto == idProducto)
                .OrderByDescending(p => p.Ano)
                .ThenByDescending(p => p.Mes)
                .Take(limit)
                .Select(p => new {
                    Periodo = $"{p.Mes}/{p.Ano}",
                    p.ConsumoPredicho,
                    p.StockMinimoCalculado,
                    p.StockIdealCalculado,
                    p.MetodoUsado,
                    p.FechaCalculo
                })
                .ToListAsync();

            return Ok(predicciones);
        }

        [HttpGet("estado-actual/{idProducto}")]
        public async Task<IActionResult> GetEstadoActual(int idProducto)
        {
            var producto = await _db.Producto
                .Include(p => p.Inventario)
                .FirstOrDefaultAsync(p => p.Id_producto == idProducto);

            if (producto == null) return NotFound();

            var consumoDiario = await _db.Pedidos
                .Where(p => p.IdProducto == idProducto &&
                           p.Estado == "Entregado" &&
                           p.FechaRecepcion >= DateTime.Now.AddMonths(-3))
                .GroupBy(p => 1)
                .Select(g => new {
                    Total = g.Sum(p => p.Cantidad),
                    Dias = g.Select(p => p.FechaRecepcion.Value).Distinct().Count()
                })
                .FirstOrDefaultAsync();

            var consumo = consumoDiario != null && consumoDiario.Dias > 0 ?
                consumoDiario.Total / (double)consumoDiario.Dias : 0;

            var diasHastaMinimo = producto.Inventario.StockActual / consumo;

            return Ok(new
            {
                Producto = new
                {
                    producto.Id_producto,
                    producto.Nombre_producto,
                    producto.Metodoprediccion
                },
                Inventario = new
                {
                    producto.Inventario.StockActual,
                    producto.Inventario.StockMinimo,
                    producto.Inventario.StockIdeal,
                    producto.Inventario.UltimaEntrada
                },
                Consumo = new
                {
                    PromedioDiario = Math.Round(consumo, 2),
                    DiasHastaMinimo = Math.Round(diasHastaMinimo, 1)
                }
            });
        }
    }
}
