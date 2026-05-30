using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Materpiece.Data; 

namespace Materpiece.Controllers
{
    public class MapController : Controller
    { 
        private readonly ApplicationDbContext _context;

        public MapController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Map
        public IActionResult Index()
        {
            return View();
        }

    
        [HttpGet]
        public async Task<IActionResult> GetStationsJson()
        {
            var stations = await _context.Stations
                .Select(s => new
                {
                    s.StationId,
                    s.Name,
                    s.Address,
                    s.Description,
                    s.Latitude,
                    s.Longitude,
                    s.TotalSlots,
                    // Dynamically calculate remaining slots matching your table states
                    AvailableSlots = _context.ChargerSlots.Count(cs => cs.StationId == s.StationId && cs.Status == SlotStatus.Available)
                })
                .ToListAsync();

            return Json(stations);
        }
    }
}