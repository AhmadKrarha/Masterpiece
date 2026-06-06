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
    [ApiController]
    [Route("[controller]")]
    public class ReviewsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public ReviewsController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpPost("SubmitReview")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitReview([FromBody] SubmitReviewDto model)
        {
            if (model == null || !ModelState.IsValid)
            {
                return Json(new { success = false, message = "Invalid review data." });
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "User not found." });
            }

            // Verify the booking belongs to the user and is completed
            var booking = await _context.Bookings
                .FirstOrDefaultAsync(b => b.BookingId == model.BookingId && b.UserId == userId);

            if (booking == null)
            {
                return Json(new { success = false, message = "Booking not found or unauthorized." });
            }

            if (booking.Status != BookingStatus.Completed)
            {
                return Json(new { success = false, message = "You can only review completed sessions." });
            }

            // Check if review already exists
            bool exists = await _context.Reviews.AnyAsync(r => r.BookingId == model.BookingId);
            if (exists)
            {
                return Json(new { success = false, message = "You have already reviewed this session." });
            }

            var review = new Review
            {
                StationId = model.StationId,
                UserId = userId,
                BookingId = model.BookingId,
                Rating = model.Rating,
                Comment = model.Comment,
                CreatedAt = DateTime.UtcNow
            };

            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }
    }

    public class SubmitReviewDto
    {
        public int BookingId { get; set; }
        public int StationId { get; set; }
        public int Rating { get; set; }
        public string Comment { get; set; } = string.Empty;
    }
}
