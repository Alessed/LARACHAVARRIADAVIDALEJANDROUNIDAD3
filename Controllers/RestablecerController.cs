using System;
using System.Threading.Tasks;
using Dentalara.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Dentalara.Data;
using Microsoft.AspNetCore.Http;

namespace Dentalara.Controllers
{
    public class RestablecerController : Controller
    {
        private readonly ApplicationDbContext _context;

        public RestablecerController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Restablecer/Restablecer
        public IActionResult Restablecer()
        {
            // Obtener token y email de la sesión
            var token = HttpContext.Session.GetString("TokenValido");
            var email = HttpContext.Session.GetString("EmailUsuario");

            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(email))
            {
                // Redirigir al inicio del proceso si no hay sesión
                return RedirectToAction("Recuperar", "Recuperar");
            }

            return View("~/Views/Recuperar/Restablecer.cshtml");
        }

        // POST: /Restablecer/Restablecer
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Restablecer(string nuevaContrasena, string confirmarContrasena)
        {
            try
            {
                // Leer token y email desde sesión
                var token = HttpContext.Session.GetString("TokenValido");
                var email = HttpContext.Session.GetString("EmailUsuario");

                if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(email))
                {
                    return Json(new
                    {
                        success = false,
                        message = "Sesión inválida. Por favor inicie el proceso nuevamente."
                    });
                }

                if (nuevaContrasena != confirmarContrasena)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Las contraseñas no coinciden"
                    });
                }

                var usuario = await _context.Usuarios
                    .FirstOrDefaultAsync(u =>
                        u.Email == email &&
                        u.TokenRecuperacion == token &&
                        u.FechaExpiracionToken > DateTime.Now);

                if (usuario == null)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Token inválido o expirado"
                    });
                }

                // Actualizar contraseña (considera usar hashing en producción)
                usuario.Contrasena = nuevaContrasena;
                usuario.TokenRecuperacion = null;
                usuario.FechaExpiracionToken = null;

                _context.Update(usuario);
                await _context.SaveChangesAsync();

                // Limpiar sesión de recuperación
                HttpContext.Session.Remove("TokenValido");
                HttpContext.Session.Remove("EmailUsuario");

                return Json(new
                {
                    success = true,
                    redirectUrl = Url.Action("Login", "Auth")
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = "Error interno: " + ex.Message
                });
            }
        }
    }
}
