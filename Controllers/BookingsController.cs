using Materpiece.ViewModels;
using Materpiece.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;


namespace Materpiece.Controllers
{
    [Authorize] 
    public class BookingsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public BookingsController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        
        [HttpGet]
        public async Task<IActionResult> CheckAvailability(int slotId, DateTime start, DateTime end)
        {
            if (start >= end || start < DateTime.Now)
            {
                return Json(new { available = false, message = "Invalid time range selected." });
            }


            bool isOverlapping = await _context.Bookings
               .AnyAsync(b => b.SlotId == slotId &&
                              (b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.Pending) &&
                              start < b.EndTime && end > b.StartTime);

            return Json(new { available = !isOverlapping });
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reserve(CreateBookingViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Invalid booking details submitted.";
                return RedirectToAction("Index", "Map");
            }

            if (model.StartTime >= model.EndTime || model.StartTime < DateTime.Now)
            {
                TempData["Error"] = "The booking window must be in the future, and end time must exceed start time.";
                return RedirectToAction("Index", "Map");
            }

            var userId = _userManager.GetUserId(User);

            var strategy = _context.Database.CreateExecutionStrategy();

            bool bookingCreated = false;

            await strategy.ExecuteAsync(async () =>
            {
                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    try
                    {
                         
                        bool dynamicConflict = await _context.Bookings
                           .AnyAsync(b => b.SlotId == model.SlotId &&
                                          (b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.Pending) &&
                                          model.StartTime < b.EndTime && model.EndTime > b.StartTime);

                        if (dynamicConflict)
                        {

                            return;
                        }

 
                        var booking = new Booking
                        {
                            UserId = userId,
                            SlotId = model.SlotId,
                            StartTime = model.StartTime,
                            EndTime = model.EndTime,
                            Status = BookingStatus.Pending 
                        };

                        _context.Bookings.Add(booking);
                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();

                        bookingCreated = true;
                    }
                    catch (Exception)
                    {
                        await transaction.RollbackAsync();
                    }
                }
            });

            if (!bookingCreated)
            {
                TempData["Error"] = "This slot was just reserved by another driver. Please pick a different slot or time window.";
                return RedirectToAction("Index", "Map");
            }

            TempData["Success"] = "Slot locked! Redirecting to payment processing...";

            // NEXT STEP HANDOFF: We will pass this to Stripe payment portal
            return RedirectToAction("ProcessPayment", "Payment", new { bookingId = 1 /* Change to booking.BookingId later */ });
        }
    }
}