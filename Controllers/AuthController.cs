using Microsoft.AspNetCore.Mvc;
using Dentalara.Models;
using System.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Dentalara.Models.ViewModels;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;

namespace Dentalara.Controllers
{
    [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
    public class AuthController : Controller
    {
        private readonly string _connectionString;

        public AuthController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var query = @"SELECT IdUsuario, Nombre, TipoUsuario 
                      FROM Usuarios 
                      WHERE Email = @Email AND Contrasena = @Contrasena";

                using (var cmd = new SqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@Email", model.Email);
                    cmd.Parameters.AddWithValue("@Contrasena", model.Contrasena);

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            var usuarioId = reader.GetInt32(0);
                            var nombreUsuario = reader.GetString(1);
                            var tipoUsuario = reader.GetString(2);

                            // ===============================
                            // 1. Generar token de sesión único
                            // ===============================
                            var sessionToken = Guid.NewGuid().ToString();

                            // Guardarlo en la base de datos
                            reader.Close(); // Cerramos el reader antes de hacer UPDATE
                            using (var updateCmd = new SqlCommand(
                                "UPDATE Usuarios SET SessionToken = @SessionToken WHERE IdUsuario = @IdUsuario", connection))
                            {
                                updateCmd.Parameters.AddWithValue("@SessionToken", sessionToken);
                                updateCmd.Parameters.AddWithValue("@IdUsuario", usuarioId);
                                await updateCmd.ExecuteNonQueryAsync();
                            }

                            // ===============================
                            // 2. Guardar datos en sesión
                            // ===============================
                            HttpContext.Session.SetInt32("UsuarioId", usuarioId);
                            HttpContext.Session.SetString("NombreUsuario", nombreUsuario);
                            HttpContext.Session.SetString("TipoUsuario", tipoUsuario);
                            HttpContext.Session.SetString("SessionToken", sessionToken);

                            // ===============================
                            // 3. Crear claims para cookie
                            // ===============================
                            var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.NameIdentifier, usuarioId.ToString()),
                        new Claim(ClaimTypes.Name, nombreUsuario),
                        new Claim(ClaimTypes.Role, tipoUsuario),
                        new Claim("SessionToken", sessionToken) // Token para validar multisesión
                    };

                            var authProperties = new AuthenticationProperties
                            {
                                IsPersistent = model.RememberMe
                            };

                            await HttpContext.SignInAsync(
                                CookieAuthenticationDefaults.AuthenticationScheme,
                                new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)),
                                authProperties);

                            // ===============================
                            // 4. Redirección según rol
                            // ===============================
                            return tipoUsuario switch
                            {
                                "Administrador" => RedirectToAction("Index", "Admin"),
                                "Dentista" => RedirectToAction("Index", "Dentista"),
                                _ => RedirectToAction("Index", "VistaUsuario")
                            };
                        }
                    }
                }
            }

            ModelState.AddModelError("", "Credenciales inválidas");
            return View(model);
        }


        [HttpGet]
        public IActionResult Registro()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Registro(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                // Verificar si el email ya existe
                var checkQuery = "SELECT COUNT(*) FROM Usuarios WHERE Email = @Email";
                using (var checkCmd = new SqlCommand(checkQuery, connection))
                {
                    checkCmd.Parameters.AddWithValue("@Email", model.Email);
                    var count = (int)await checkCmd.ExecuteScalarAsync();

                    if (count > 0)
                    {
                        ModelState.AddModelError("Email", "El correo ya está registrado");
                        return View(model);
                    }
                }

                // Registrar nuevo usuario (siempre como Paciente)
                var insertQuery = @"INSERT INTO Usuarios (Nombre, Email, Contrasena, TipoUsuario) 
                                   VALUES (@Nombre, @Email, @Contrasena, 'Paciente')";

                using (var insertCmd = new SqlCommand(insertQuery, connection))
                {
                    insertCmd.Parameters.AddWithValue("@Nombre", model.Nombre);
                    insertCmd.Parameters.AddWithValue("@Email", model.Email);
                    insertCmd.Parameters.AddWithValue("@Contrasena", model.Contrasena);

                    await insertCmd.ExecuteNonQueryAsync();
                }
            }

            return RedirectToAction("Login");
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            // Limpiar sesión
            HttpContext.Session.Clear();

            // Cerrar cookie de autenticación
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            // Evitar cache del navegador
            Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate, max-age=0";
            Response.Headers["Pragma"] = "no-cache";
            Response.Headers["Expires"] = "-1";

            return RedirectToAction("Index", "Home");
        }



        [HttpGet]
        public IActionResult AccesoDenegado()
        {
            return View();
        }
    }
}