using System.Security.Claims;
using Materpiece.Data;
using Materpiece.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Materpiece.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public ProfileController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: /Profile
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound("User not found.");
            }

            var bookings = await _context.Bookings
                .Include(b => b.ChargerSlot)
                    .ThenInclude(cs => cs!.Station)
                .Where(b => b.UserId == userId)
                .OrderByDescending(b => b.StartTime)
                .ToListAsync();

            // Auto-complete confirmed bookings that have already ended
            var now = DateTime.Now;
            bool dbChanged = false;
            foreach (var booking in bookings)
            {
                if (booking.Status == BookingStatus.Confirmed && booking.EndTime < now)
                {
                    booking.Status = BookingStatus.Completed;
                    dbChanged = true;
                }
            }
            if (dbChanged)
            {
                await _context.SaveChangesAsync();
            }

            // Calculate Charging Statistics
            int totalBookings = bookings.Count;
            double totalEnergyKwh = bookings
                .Where(b => b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.Completed)
                .Sum(b => b.EnergyRequestedKwh);
            double co2SavedKg = totalEnergyKwh * 0.40; // Approx 0.4kg CO2 saved per kWh vs gasoline

            var reviewedBookingIds = await _context.Reviews
                .Where(r => r.UserId == userId)
                .Select(r => r.BookingId)
                .ToListAsync();

            // Pass stats and user info to ViewBag
            ViewBag.Email = user.Email;
            ViewBag.PhoneNumber = user.PhoneNumber ?? "";
            ViewBag.TotalBookings = totalBookings;
            ViewBag.TotalEnergyKwh = totalEnergyKwh;
            ViewBag.Co2SavedKg = co2SavedKg;
            ViewBag.ReviewedBookingIds = reviewedBookingIds;

            return View(bookings);
        }

        // POST: /Profile/UpdatePhone
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdatePhone([FromBody] UpdatePhoneModel model)
        {
            if (model == null || string.IsNullOrEmpty(model.PhoneNumber))
            {
                return Json(new { success = false, message = "Phone number cannot be empty." });
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "Unauthorized request." });
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return Json(new { success = false, message = "User not found." });
            }

            user.PhoneNumber = model.PhoneNumber.Trim();
            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                return Json(new { success = true, message = "Phone number updated successfully!" });
            }

            var error = result.Errors.FirstOrDefault()?.Description ?? "Failed to update phone number.";
            return Json(new { success = false, message = error });
        }
    }

    public class UpdatePhoneModel
    {
        public string PhoneNumber { get; set; } = string.Empty;
    }
}
