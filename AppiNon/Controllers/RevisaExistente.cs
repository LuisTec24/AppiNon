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
    public class RevisaExistenteController : ControllerBase
    {
        private readonly PinonBdContext _context;
        private readonly string secretkey;

        public RevisaExistenteController(IConfiguration conf, PinonBdContext context)
        {
            secretkey = conf.GetSection("settings")["secretkey"];
            _context = context;
        }

        [HttpPost("Validar")]
        [AllowAnonymous]
        public async Task<IActionResult> VerificarUsuario([FromBody] VerificarUsuarioRequest request)
        {
            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Correo == request.Correo);

            if (usuario != null && BCrypt.Net.BCrypt.Verify(request.ContraseñaH, usuario.Contraseña_hash))
            {
                var keyBytes = Encoding.ASCII.GetBytes(secretkey);
                var claims = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, usuario.Correo),
                      new Claim(ClaimTypes.Name, usuario.Nombre), // ← AQUI agregas el nombre
                    new Claim(ClaimTypes.Role, usuario.Rol_id.ToString())
                });

                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = claims,
                    Expires = DateTime.UtcNow.AddMinutes(30),
                    SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(keyBytes), SecurityAlgorithms.HmacSha256Signature)
                };

                var tokenHandler = new JwtSecurityTokenHandler();
                var token = tokenHandler.CreateToken(tokenDescriptor);

                return Ok(tokenHandler.WriteToken(token));
            }

            return Unauthorized("Credenciales inválidas");
        }

        [HttpGet]
        [Authorize(Roles = "1")]
        public async Task<ActionResult<IEnumerable<Usuarios>>> GetUsuarios()
        {
            return await _context.Usuarios.ToListAsync();
        }

        [HttpPost]
        [Authorize(Roles = "1")]
        public async Task<IActionResult> CrearUsuario([FromBody] Usuarios nuevoUsuario)
        {
            if (await _context.Usuarios.AnyAsync(u => u.Correo == nuevoUsuario.Correo))
                return Conflict("El correo ya existe");

            nuevoUsuario.ID = 0;
            nuevoUsuario.Contraseña_hash = BCrypt.Net.BCrypt.HashPassword(nuevoUsuario.Contraseña_hash);

            _context.Usuarios.Add(nuevoUsuario);
            await _context.SaveChangesAsync();
            await RegistrarBitacora("INSERT", "Usuarios", nuevoUsuario.ID, $"Usuario creado: {nuevoUsuario.Correo}");

            return Ok(nuevoUsuario);
        }



        [HttpGet("VerificarCorreo")]
        [Authorize(Roles = "1")]
        public async Task<IActionResult> VerificarCorreo([FromQuery] string correo)
        {
            if (string.IsNullOrWhiteSpace(correo))
            {
                return BadRequest("El correo no puede estar vacío");
            }

            // Verifica si el correo existe en la tabla de usuarios (o proveedores, según tu modelo)
            bool existe = await _context.Usuarios.AnyAsync(u => u.Correo == correo);

            return Ok(new { existe });
        }


        [HttpPut("{correo}")]
        [Authorize(Roles = "1")]

        public async Task<IActionResult> PutUsuarioPorCorreo(string correo, Usuarios usuarioActualizado)
        {
            var usuarioExistente = await _context.Usuarios.FirstOrDefaultAsync(u => u.Correo == correo);
            if (usuarioExistente == null)
                return NotFound();

            usuarioExistente.Nombre = usuarioActualizado.Nombre;
            usuarioExistente.Contraseña_hash = usuarioActualizado.Contraseña_hash;
            usuarioExistente.Rol_id = usuarioActualizado.Rol_id;

            await _context.SaveChangesAsync();
            await RegistrarBitacora("UPDATE", "Usuarios", usuarioExistente.ID, $"Usuario actualizado: {usuarioExistente.Correo}");

            return NoContent();
        }

        [HttpDelete("{correo}")]
        [Authorize(Roles = "1")]
        public async Task<IActionResult> DeleteUsuario(string correo)
        {
            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Correo == correo);
            if (usuario == null)
                return NotFound();

            _context.Usuarios.Remove(usuario);
            await _context.SaveChangesAsync();
            await RegistrarBitacora("DELETE", "Usuarios", usuario.ID, $"Usuario eliminado: {usuario.Correo}");

            return NoContent();
        }

        private async Task RegistrarBitacora(string tipo, string entidad, int idEntidad, string descripcion)
        {
            var bitacora = new Bitacora
            {
                Fecha = DateTime.Now,
                Tipo_de_Modificacion = tipo,
                ID_Usuario = 1, // puedes cambiar esto por el ID del usuario autenticado
                Entidad = entidad,
                ID_Entidad = idEntidad,
                Descripcion = descripcion
            };

            _context.Bitacora.Add(bitacora);
            await _context.SaveChangesAsync();
        }

        public class VerificarUsuarioRequest
        {
            public string Correo { get; set; }
            public string ContraseñaH { get; set; }
        }
    }
}
