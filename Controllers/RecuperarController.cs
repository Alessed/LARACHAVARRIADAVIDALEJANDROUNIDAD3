using System;
using System.Linq;
using System.Threading.Tasks;
using Dentalara.Helpers;
using Dentalara.Models;
using MailKit.Net.Smtp;
using MimeKit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Dentalara.Data;

namespace Dentalara.Controllers
{
    public class RecuperarController : Controller
    {
        private readonly ApplicationDbContext _context;

        public RecuperarController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Recuperar/Recuperar
        public IActionResult Recuperar()
        {
            return View();
        }

        // POST: /Recuperar/Recuperar
        [HttpPost]
        public async Task<IActionResult> Recuperar(string correo)
        {
            try
            {
                // CONSULTA SEGURA 1: Verificar existencia del email
                var emailExiste = await _context.Usuarios
                    .AsNoTracking()
                    .AnyAsync(u => u.Email == correo);

                if (!emailExiste)
                {
                    ViewBag.Mensaje = "No se encontró el correo.";
                    return View();
                }

                // CONSULTA SEGURA 2: Obtener solo datos necesarios
                var usuario = await _context.Usuarios
                    .Where(u => u.Email == correo)
                    .Select(u => new { u.Email, u.Nombre })
                    .FirstOrDefaultAsync();

                // Generar token
                var token = TokenHelper.GenerarToken();

                // ACTUALIZACIÓN DIRECTA CON SQL
                await _context.Database.ExecuteSqlInterpolatedAsync(
                    $@"UPDATE Usuarios 
                   SET TokenRecuperacion = {token}, 
                       FechaExpiracionToken = {DateTime.Now.AddHours(1)}
                   WHERE Email = {correo}");

                await EnviarCorreoRecuperacion(new Usuario
                {
                    Email = usuario.Email,
                    Nombre = usuario.Nombre
                }, token);

                TempData["EmailRecuperacion"] = usuario.Email;
                return RedirectToAction("IngresarToken");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en Recuperar: {ex}");
                ViewBag.Mensaje = "Ocurrió un error al procesar la solicitud";
                return View();
            }
        }

        [HttpGet]
        public IActionResult IngresarToken()
        {
            var email = TempData["EmailRecuperacion"]?.ToString();
            if (string.IsNullOrEmpty(email))
            {
                return RedirectToAction("Recuperar");
            }
            ViewBag.Email = email;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> IngresarToken(string email, string token)
        {
            try
            {
                var tokenValido = await _context.Usuarios
                    .FromSqlInterpolated($@"
                SELECT * FROM Usuarios 
                WHERE Email = {email} 
                AND TokenRecuperacion = {token}
                AND FechaExpiracionToken > {DateTime.Now}")
                    .AnyAsync();

                if (!tokenValido)
                {
                    ViewBag.Error = "Token inválido o expirado";
                    ViewBag.Email = email;
                    return View();
                }

                // Guardar en sesión
                HttpContext.Session.SetString("TokenValido", token);
                HttpContext.Session.SetString("EmailUsuario", email);

                return RedirectToAction("Restablecer", "Restablecer");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en IngresarToken: {ex}");
                ViewBag.Error = "Error al validar el token";
                ViewBag.Email = email;
                return View();
            }
        }


        private async Task EnviarCorreoRecuperacion(Usuario usuario, string token)
        {
            var mensaje = new MimeMessage();
            mensaje.From.Add(new MailboxAddress("Dental Center", "tu_correo@gmail.com"));
            mensaje.To.Add(new MailboxAddress(usuario.Nombre, usuario.Email));
            mensaje.Subject = "Recupera tu contraseña";

            mensaje.Body = new TextPart("html")
            {
                Text = $@"
                <p>Hola {usuario.Nombre},</p>
                <p>Hemos recibido una solicitud para restablecer tu contraseña.</p>
                <p>Tu código de verificación es: <strong>{token}</strong></p>
                <p>Este código expirará en 1 hora.</p>
                <p>Si no solicitaste este cambio, por favor ignora este mensaje.</p>"
            };

            using (var smtp = new SmtpClient())
            {
                await smtp.ConnectAsync("smtp.gmail.com", 587, false);
                await smtp.AuthenticateAsync("proyectoalex16@gmail.com", "anyiktvcvclrfore");
                await smtp.SendAsync(mensaje);
                await smtp.DisconnectAsync(true);
            }
        }
    }
}