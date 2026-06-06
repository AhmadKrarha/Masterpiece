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
                    .OrderByDescending(s => s.StationId)
                    .Take(5)
                    .ToListAsync();

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

            // Data Ownership Gate: Only station owners can access the owner dashboard
            bool isOwner = await _context.Stations.AnyAsync(s => s.OwnerId == userId);
            if (isOwner)
            {
                var myStations = await _context.Stations
                    .Include(s => s.ChargerSlots)
                    .Where(s => s.OwnerId == userId)
                    .ToListAsync();

                var myStationIds = myStations.Select(s => s.StationId).ToList();

                var totalBookings = await _context.Bookings
                    .CountAsync(b => myStationIds.Contains(b.ChargerSlot!.StationId));

                var totalRevenue = await _context.Payments
                    .Where(p => p.Status == "Succeeded" &&
                                myStationIds.Contains(p.Booking!.ChargerSlot!.StationId))
                    .SumAsync(p => (decimal?)p.Amount) ?? 0m;

                var ownerViewModel = new DashboardOwnerViewModel
                {
                    TotalStations = myStations.Count,
                    TotalActiveSlots = myStations.Sum(s => s.ChargerSlots.Count(cs => cs.Status == SlotStatus.Available)),
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
