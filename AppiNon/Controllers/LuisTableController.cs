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
    [Route("api/[controller]")]
    [Authorize]


    [ApiController]
    public class LuisTableController : ControllerBase
    {
        private readonly PinonBdContext _context;

        public LuisTableController(PinonBdContext context)
        {
            _context = context;
        }

        // GET: api/LuisTableController
        [HttpGet]
        [Authorize(Roles = "1,2")]
        public async Task<ActionResult<IEnumerable<LuisTable>>> GetLuisTables()
        {
            return await _context.LuisTables.ToListAsync();
        }

        // GET: api/LuisTableController/5
        [HttpGet("{id}")]
        public async Task<ActionResult<LuisTable>> GetLuisTable(int id)
        {
            var LuisTable = await _context.LuisTables.FindAsync(id);

            if (LuisTable == null)
            {
                return NotFound();
            }

            return LuisTable;
        }

        // PUT: api/LuisTableController/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        [Authorize(Roles = "1")] //
        public async Task<IActionResult> PutLuisTable(int id, LuisTable LuisTable)
        {
            if (id != LuisTable.Id)
            {
                return BadRequest();
            }

            _context.Entry(LuisTable).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!LuisTableExists(id))
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

        // POST: api/LuisTableController
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        [Authorize(Roles = "1")] //
        public async Task<ActionResult<LuisTable>> PostLuisTable(LuisTable LuisTable)
        {
            _context.LuisTables.Add(LuisTable);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetLuisTable", new { id = LuisTable.Id }, LuisTable);
        }

        // DELETE: api/LuisTableController/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "1")] //
        public async Task<IActionResult> DeleteLuisTable(int id)
        {
            var LuisTable = await _context.LuisTables.FindAsync(id);
            if (LuisTable == null)
            {
                return NotFound();
            }

            _context.LuisTables.Remove(LuisTable);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool LuisTableExists(int id)
        {
            return _context.LuisTables.Any(e => e.Id == id);
        }
    }
}
