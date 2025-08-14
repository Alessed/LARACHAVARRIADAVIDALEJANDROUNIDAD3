using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Authorize(Roles = "Administrador")]
public class AdminController : Controller
{
    public IActionResult Index()
    {
        ViewBag.NombreUsuario = User.Identity.Name; // Obtiene el nombre del claim
        return View();
    }
}