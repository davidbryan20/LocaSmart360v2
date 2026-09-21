using System.Diagnostics;
using LocaSmart360.Data;
using LocaSmart360.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LocaSmart360.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly LocaSmartDbContext _context;

        public HomeController(ILogger<HomeController> logger, LocaSmartDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            bool isOnline = false;
            try
            {
                isOnline = await _context.Database.CanConnectAsync();
            }
            catch
            {
                isOnline = false;
            }

            ViewBag.SistemaOnline = isOnline;
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [HttpGet]
        public IActionResult Sobre()
        {
            return View();
        }
    }
}