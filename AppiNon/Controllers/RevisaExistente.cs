using AppiNon.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace AppiNon.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RevisaExistente : ControllerBase
    {

        private readonly PinonBdContext _context;
        private readonly string secretkey;

        public RevisaExistente(IConfiguration conf, PinonBdContext context)
        {
            secretkey = conf.GetSection("settings").GetSection("secretkey").ToString();
            _context = context;
        }


        [HttpPost]
        [AllowAnonymous]
        [Route("Validar")]
        public async Task<IActionResult> VerificarUsuario([FromBody] VerificarUsuarioRequest request)
        {
            // Obtener el usuario basado en el correo
            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.correo == request.Correo);


            if (usuario != null && BCrypt.Net.BCrypt.Verify(request.ContraseñaH, usuario.contraseña_hash))
            {

                var keyBytes = Encoding.ASCII.GetBytes(secretkey);
                var claims = new ClaimsIdentity();

                claims.AddClaim(new Claim(ClaimTypes.NameIdentifier, request.Correo));
                claims.AddClaim(new Claim(ClaimTypes.Role, usuario.rol_id.ToString()));  // Añades el rol al token

                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = claims,
                    Expires = DateTime.UtcNow.AddMinutes(5),
                    SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(keyBytes), SecurityAlgorithms.HmacSha256Signature)
                };

                var tokenHandler = new JwtSecurityTokenHandler();
                var tokenConfig = tokenHandler.CreateToken(tokenDescriptor);
                string tokencreado = tokenHandler.WriteToken(tokenConfig);
                return StatusCode(StatusCodes.Status200OK, tokencreado);
            }
            else
            {
                return StatusCode(StatusCodes.Status401Unauthorized, " ");
            }
        }

        public class VerificarUsuarioRequest
        {
            public string Correo { get; set; }
            public string ContraseñaH { get; set; }

        }

        [Authorize]
        //[Route("Validar")]
        [HttpGet]
        [Authorize(Roles = "1")]
        public async Task<ActionResult<IEnumerable<Usuarios>>> GetLuisTables()
        {
            return await _context.Usuarios.ToListAsync();
        }



        [HttpPut("{correo}")]
        [Authorize(Roles = "1")]
        public async Task<IActionResult> PutUsuarioPorCorreo(string correo, Usuarios usuarioActualizado)
        {
            var usuarioExistente = await _context.Usuarios.FirstOrDefaultAsync(u => u.correo == correo);

            if (usuarioExistente == null)
            {
                return NotFound();
            }

            // actualiza manualmente los campos necesarios
            usuarioExistente.nombre = usuarioActualizado.nombre;
            usuarioExistente.contraseña_hash = usuarioActualizado.contraseña_hash;
            usuarioExistente.rol_id = usuarioActualizado.rol_id;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                throw;
            }

            return NoContent();
        }

        [HttpDelete("{correo}")]
        [Authorize(Roles = "1")]
        public async Task<IActionResult> DeleteUsuario(string correo)
        {
            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.correo == correo);
            if (usuario == null)
            {
                return NotFound();
            }
            _context.Usuarios.Remove(usuario);
            await _context.SaveChangesAsync();
            return NoContent();
        }

    }
}