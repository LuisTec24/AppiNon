using AppiNon.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AppiNon.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BitacoraController : ControllerBase
    {
        private readonly PinonBdContext _context;

            public BitacoraController(PinonBdContext context)
            {
                _context = context;
            }

            // GET: api/bitacora
            [HttpGet]
            public async Task<ActionResult<IEnumerable<Bitacora>>> GetBitacora()
            {
                var registros = await _context.Bitacora
                    .OrderByDescending(b => b.Fecha)
                    .ToListAsync();

                return Ok(registros);
            }
        }
    
}
