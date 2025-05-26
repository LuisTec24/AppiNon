//using AppiNon.Models;
//using Microsoft.AspNetCore.Http;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.EntityFrameworkCore;

//namespace AppiNon.Controllers
//{
//    [Route("api/[controller]")]
//    [ApiController]
//    public class Twilio : ControllerBase
//    {


//        [HttpPost("webhook/whatsapp-respuesta")]
//        public async Task<IActionResult> RecibirRespuesta()
//        {
//            var form = await Request.ReadFormAsync();
//            string mensaje = form["Body"];
//            string numero = form["From"];

//            // buscar el pedido pendiente asociado a ese número
//            var pedido = await _context.Pedidos
//                .Where(p => p.TelefonoProveedor == numero && p.Estado == "Pendiente")
//                .OrderByDescending(p => p.FechaPedido)
//                .FirstOrDefaultAsync();

//            if (pedido == null)
//                return Ok(); // no hacer nada si no hay pedido pendiente

//            if (mensaje.Trim().ToLower().Contains("sí"))
//                pedido.Estado = "Confirmado";
//            else if (mensaje.Trim().ToLower().Contains("no"))
//                pedido.Estado = "Rechazado";

//            await _context.SaveChangesAsync();
//            return Ok();
//        }


//        public async Task EnviarPedidoPorWhatsapp(Pedido pedido, string telefonoProveedor)
//        {
//            var accountSid = "TU_ACCOUNT_SID";
//            var authToken = "TU_AUTH_TOKEN";
//            var twilioClient = new TwilioRestClient(accountSid, authToken);

//            string mensaje = $"Pedido #{pedido.Id}: {pedido.Cantidad} unidades de {pedido.NombreProducto}. Responde 'sí' para confirmar o 'no' para rechazar.";

//            var message = await MessageResource.CreateAsync(
//                body: mensaje,
//                from: new PhoneNumber("whatsapp:+14155238886"), // número de Twilio
//                to: new PhoneNumber("whatsapp:" + telefonoProveedor),
//                client: twilioClient
//            );
//        }


//    }
//}
