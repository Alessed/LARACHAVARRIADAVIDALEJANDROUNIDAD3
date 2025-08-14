using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Authorize(Roles = "Dentista")]
public class DentistaController : Controller
{
    public IActionResult Index()
    {
        ViewBag.NombreUsuario = User.Identity.Name;
        return View();
    }
}