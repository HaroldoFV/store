using Microsoft.AspNetCore.Mvc;

namespace CartMicroservice;

public class CartController : Controller
{
    // GET
    public IActionResult Index()
    {
        return View();
    }
}