using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Materpiece.Data; 
using System.Security.Claims;

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
        public async Task<IActionResult> GetActiveBookingJson()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { hasBooking = false });
            }

            var now = DateTime.Now;

            // Fetch the user's nearest future booking or current active booking
            var booking = await _context.Bookings
                .Include(b => b.ChargerSlot)
                    .ThenInclude(cs => cs!.Station)
                .Where(b => b.UserId == userId && 
                            b.Status == BookingStatus.Confirmed && 
                            b.EndTime > now)
                .OrderBy(b => b.StartTime)
                .FirstOrDefaultAsync();

            if (booking == null)
            {
                return Json(new { hasBooking = false });
            }

            return Json(new
            {
                hasBooking = true,
                bookingId = booking.BookingId,
                stationName = booking.ChargerSlot?.Station?.Name ?? "EV Station",
                slotNumber = booking.ChargerSlot?.SlotNumber ?? "Slot",
                startTime = booking.StartTime.ToString("o"),
                endTime = booking.EndTime.ToString("o"),
                serverTime = now.ToString("o")
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetStationsJson()
        {
            var now = DateTime.Now;

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
                    // Dynamically calculate remaining slots matching your table states (subtract active confirmed/pending bookings)
                    AvailableSlots = _context.ChargerSlots.Count(cs => 
                        cs.StationId == s.StationId && 
                        cs.Status == SlotStatus.Available &&
                        !_context.Bookings.Any(b => 
                            b.SlotId == cs.SlotId && 
                            (b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.Pending) &&
                            now >= b.StartTime && 
                            now <= b.EndTime)
                    )
                })
                .ToListAsync();

            return Json(stations);
        }
    }
}