using Materpiece.Data;
using Materpiece.Models;
using Materpiece.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Materpiece.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public DashboardController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return Challenge(); // Gracefully handles expired sessions
            }

            var isAdmin = User.IsInRole("Admin");

            if (isAdmin)
            {
                var totalUsers = await _userManager.Users.CountAsync();
                var totalStations = await _context.Stations.CountAsync();
                var totalBookings = await _context.Bookings.CountAsync();

                var totalRevenue = await _context.Payments
                    .Where(p => p.Status == "Succeeded")
                    .SumAsync(p => (decimal?)p.Amount) ?? 0m;

                var recentStations = await _context.Stations
                    .OrderByDescending(s => s.StationId).Take(5).ToListAsync();

                var adminViewModel = new DashboardAdminViewModel
                {
                    TotalUsers = totalUsers,
                    TotalStations = totalStations,
                    TotalBookings = totalBookings,
                    TotalRevenue = totalRevenue,
                    RecentStations = recentStations
                };

                return View("AdminDashboard", adminViewModel);
            }

            var myStations = await _context.Stations
                .Include(s => s.ChargerSlots)
                .Where(s => s.OwnerId == userId)
                .ToListAsync();

            if (myStations.Any())
            {
                var myStationIds = myStations.Select(s => s.StationId).ToList();

                // 1. Calculate active slots directly via database query instead of in-memory LINQ
                var totalActiveSlots = await _context.ChargerSlots
                    .CountAsync(cs => myStationIds.Contains(cs.StationId) && cs.Status == SlotStatus.Available);

                // 2. Optimized Booking Count
                var totalBookings = await _context.Bookings
                    .CountAsync(b => b.ChargerSlot != null && myStationIds.Contains(b.ChargerSlot.StationId));

                // 3. Optimized Revenue Sum
                var totalRevenue = await _context.Payments
                    .Where(p => p.Status == "Succeeded" &&
                                p.Booking != null &&
                                p.Booking.ChargerSlot != null &&
                                myStationIds.Contains(p.Booking.ChargerSlot.StationId))
                    .SumAsync(p => (decimal?)p.Amount) ?? 0m;

                var ownerViewModel = new DashboardOwnerViewModel
                {
                    TotalStations = myStations.Count,
                    TotalActiveSlots = totalActiveSlots,
                    TotalBookings = totalBookings,
                    TotalRevenue = totalRevenue,
                    Stations = myStations
                };

                return View("OwnerDashboard", ownerViewModel);
            }

            return RedirectToAction("AccessDenied", "Home", new { area = "" });
        }
    }
}