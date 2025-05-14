using AppiNon.Models;
using Microsoft.AspNetCore.Mvc;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using System.Linq;
using System.Threading.Tasks;



[ApiController]
[Route("api/config-prediccion")]
public class ConfigPrediccionController : ControllerBase
{
    private readonly PinonBdContext _db;

    public ConfigPrediccionController(PinonBdContext db)
    {
        _db = db;
    }

    [HttpPut("{idProducto}")]
    public async Task<IActionResult> ConfigurarMetodo(
        int idProducto,
        [FromBody] ConfigPrediccionRequest request)
    {
        var producto = await _db.Producto.FindAsync(idProducto);
        if (producto == null) return NotFound();

        producto.Metodoprediccion = request.Metodo;
        await _db.SaveChangesAsync();

        return Ok(new
        {
            IdProducto = idProducto,
            MetodoPrediccion = producto.Metodoprediccion,
            Mensaje = $"Configuración actualizada a: {request.Metodo}"
        });
    }



    [HttpGet("recomendacion/{idProducto}")]
    [ResponseCache(Duration = 3600)] // Cache por 1 hora
    public async Task<IActionResult> GetRecomendacion(int idProducto)
    {
        if (idProducto <= 0)
            return BadRequest("ID de producto inválido");

        try
        {
            var producto = await _db.Producto
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id_producto == idProducto);

            if (producto == null)
                return NotFound($"Producto con ID {idProducto} no encontrado");

            var mesesConDatos = await _db.Pedidos
                .Where(p => p.IdProducto == idProducto)
                .Select(p => new { p.FechaRecepcion.Value.Year, p.FechaRecepcion.Value.Month })
                .Distinct()
                .CountAsync();

            var totalPedidos = await _db.Pedidos
                .Where(p => p.IdProducto == idProducto)
                .CountAsync();

            decimal variacion = 0;
            if (mesesConDatos >= 12)
            {
                var promediosPorMes = await _db.Pedidos
                    .Where(p => p.IdProducto == idProducto)
                    .GroupBy(p => p.FechaRecepcion.Value.Month)
                    .Select(g => new
                    {
                        Mes = g.Key,
                        Promedio = g.Average(p => p.Cantidad)
                    })
                    .ToListAsync();

                if (promediosPorMes.Any())
                {
                    var max = promediosPorMes.Max(x => x.Promedio);
                    var min = promediosPorMes.Min(x => x.Promedio);
                    variacion = max > 0 ? (decimal)((max - min) / max) : 0;
                }
            }

            var recomendacion = mesesConDatos < 6 ? "General" :
                              variacion > 0.3m ? "Mensual" : "General";

            return Ok(new
            {
                IdProducto = producto.Id_producto,
                NombreProducto = producto.Nombre_producto,
                MetodoActual = producto.Metodoprediccion,
                Historial = new
                {
                    MesesConDatos = mesesConDatos,
                    TotalPedidos = totalPedidos
                },
                VariacionEstacional = variacion,
                RecomendacionSistema = recomendacion,
                Explicacion = mesesConDatos < 6 ?
                    "Poco historial disponible (menos de 6 meses)" :
                    variacion > 0.3m ?
                    "Alta variación estacional detectada" :
                    "Consumo estable a lo largo del año"
            });
        }
        catch (Exception ex)
        {
           // _logger.LogError(ex, "Error al obtener recomendación para producto {ProductoId}", idProducto);
            return StatusCode(500, "Error interno del servidor");
        }
    } 
    


    private async Task<decimal> CalcularVariacionEstacional(int productoId)
    {
        var promedios = await _db.Pedidos
            .Where(p => p.IdProducto == productoId)
            .GroupBy(p => p.FechaRecepcion.Value.Month)
            .Select(g => new { Mes = g.Key, Promedio = g.Average(p => p.Cantidad) })
            .ToListAsync();

        if (promedios.Count < 12) return 0;

        var max = promedios.Max(x => x.Promedio);
        var min = promedios.Min(x => x.Promedio);
        return (decimal)(max - min) / (decimal)max;
    }
}

public class ConfigPrediccionRequest
{
    public string Metodo { get; set; }
}