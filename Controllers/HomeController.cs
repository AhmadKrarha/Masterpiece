using System.Diagnostics;
using Materpiece.Models;
using Microsoft.AspNetCore.Mvc;
using Materpiece.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Materpiece.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public IActionResult Index()
        {
            return RedirectToAction("Index", "Map");
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult AboutUs()
        {
            return View();
        }

        public IActionResult ContactUs()
        {
            return View();
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> PostLoginRedirect()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            bool isAdmin = User.IsInRole("Admin");
            bool isOwner = await _context.Stations.AnyAsync(s => s.OwnerId == userId);

            if (isAdmin)
            {
                return RedirectToAction("Index", "Stations", new { area = "Admin" });
            }

            if (isOwner)
            {
                return RedirectToAction("Index", "Dashboard");
            }

            // Normal user/EV driver goes to the home map
            return RedirectToAction("Index", "Map");
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
