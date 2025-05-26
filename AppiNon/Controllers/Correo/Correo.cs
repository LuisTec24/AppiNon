using System;
using System.Net;
using System.Net.Mail;

namespace AppiNon.Controllers
{
    public class Correo
    {
        public bool EnviarCorreo(string destino, string asunto, string cuerpo)
        {
            try
            {
                string remitente = "siminv.emp@gmail.com";
                string claveApp = "lsec rzci irve elct"; // ¡Usa una contraseña de aplicación válida!

                using (var smtp = new SmtpClient("smtp.gmail.com")
                {
                    Port = 587,
                    Credentials = new NetworkCredential(remitente, claveApp),
                    EnableSsl = true,
                    Timeout = 10000
                })
                using (var mensaje = new MailMessage(remitente, destino, asunto, cuerpo))
                {
                    smtp.Send(mensaje);
                    return true; // Éxito
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR: " + ex.Message);
                return false; // Fallo
            }
        }
    }
}