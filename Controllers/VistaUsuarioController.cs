using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dentalara.Controllers
{
    public class VistaUsuarioController : Controller
    {

        [Authorize]
        public IActionResult Index()
        {
            ViewBag.NombreUsuario = User.Identity.Name;
            return View();
        }

        [Authorize]
        public IActionResult Citas()
        {
            return View();
        }

        [Authorize]
        public IActionResult Ayuda()
        {
            return View();
        }

        [Authorize]
        public IActionResult Tratamientos()
        {
            return View();
        }

        [Authorize]
        public IActionResult Dentistas()
        {
            return View();
        }

        [Authorize]

        public IActionResult Contacto()
        {
            return View();
        }
    }
}
