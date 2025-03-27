using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using AppiNon.Models;

namespace AppiNon.Controllers
{
    [EnableCors("ReglasCors")]
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class GomezTableController : ControllerBase
    {
        private readonly PinonBdContext _context;

        public GomezTableController(PinonBdContext context)
        {
            _context = context;
        }

        // GET: api/GomezTableController
        [HttpGet]
        public async Task<ActionResult<IEnumerable<GomezTable>>> GetGomezTables()
        {
            return await _context.GomezTables.ToListAsync();
        }

        // GET: api/GomezTableController/5
        [HttpGet("{id}")]
        public async Task<ActionResult<GomezTable>> GetGomezTable(int id)
        {
            var GomezTable = await _context.GomezTables.FindAsync(id);

            if (GomezTable == null)
            {
                return NotFound();
            }

            return GomezTable;
        }

        // PUT: api/GomezTableController/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutGomezTable(int id, GomezTable GomezTable)
        {
            if (id != GomezTable.Id)
            {
                return BadRequest();
            }

            _context.Entry(GomezTable).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!GomezTableExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/GomezTableController
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<GomezTable>> PostGomezTable(GomezTable GomezTable)
        {
            _context.GomezTables.Add(GomezTable);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetGomezTable", new { id = GomezTable.Id }, GomezTable);
        }

        // DELETE: api/GomezTableController/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteGomezTable(int id)
        {
            var GomezTable = await _context.GomezTables.FindAsync(id);
            if (GomezTable == null)
            {
                return NotFound();
            }

            _context.GomezTables.Remove(GomezTable);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool GomezTableExists(int id)
        {
            return _context.GomezTables.Any(e => e.Id == id);
        }
    }
}
