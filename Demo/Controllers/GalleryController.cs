using Microsoft.AspNetCore.Mvc;

namespace Demo.Controllers
{
    public class GalleryController : Controller
    {
        // Ensure this is your Index action
        public IActionResult Index()
        {
            return View();  // This returns the View located at Views/Gallery/Index.cshtml
        }
    }
}
