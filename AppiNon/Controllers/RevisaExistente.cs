using AppiNon.Models;
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
        [Route("Validar")]
        public async Task<IActionResult> VerificarUsuario([FromBody] VerificarUsuarioRequest request)
        {
            var usuarioExiste = await _context.Usuarios.AnyAsync(u => u.correo == request.Correo && u.contraseña_hash == request.ContraseñaH);
            if (usuarioExiste)
            {

                var keyBytes = Encoding.ASCII.GetBytes(secretkey);
                var claims = new ClaimsIdentity();
                claims.AddClaim(new Claim(ClaimTypes.NameIdentifier, request.Correo));

                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = claims,
                    Expires = DateTime.UtcNow.AddMinutes(5),
                    SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(keyBytes), SecurityAlgorithms.HmacSha256Signature)
                };

                var tokenHandler = new JwtSecurityTokenHandler();
                var tokenConfig = tokenHandler.CreateToken(tokenDescriptor);

                string tokencreado = tokenHandler.WriteToken(tokenConfig);


                return StatusCode(StatusCodes.Status200OK, new { token = tokencreado });

            }
            else
            {

                return StatusCode(StatusCodes.Status401Unauthorized, new { token = "" });
            }



        }

        public class VerificarUsuarioRequest
        {
            public string Correo { get; set; }
            public string ContraseñaH { get; set; }
        }



    }
}
