using Materpiece.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Materpiece.Models.ViewModels;
using System.Security.Claims;
using Stripe;


namespace Materpiece.Controllers
{
    [Authorize]
    public class BookingsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IConfiguration _configuration;

        public BookingsController(ApplicationDbContext context, UserManager<IdentityUser> userManager, IConfiguration configuration)
        {
            _context = context;
            _userManager = userManager;
            _configuration = configuration;
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
        [HttpGet]
        public async Task<IActionResult> Checkout(int slotId)
        {
            // 1. Fetch using your exact SlotId primary key and join Station
            var slot = await _context.ChargerSlots
                .Include(s => s.Station)
                .FirstOrDefaultAsync(s => s.SlotId == slotId);

            if (slot == null)
            {
                return NotFound();
            }

            // 2. Pricing calculation based on your ChargerType enum (FastDC vs NormalAC)
            decimal determinedPrice = 3.50m; // Default NormalAC price
            if (slot.Type == ChargerType.FastDC)
            {
                determinedPrice = 6.00m; // Premium FastDC price
            }

            // 3. Map safely to the ViewModel
            var viewModel = new BookingCheckoutViewModel
            {
                ChargerSlotId = slot.SlotId, // Updated to SlotId
                StationName = slot.Station != null ? slot.Station.Name : "EV Charging Station",
                LocationDescription = slot.Station != null ? slot.Station.Address : "Amman, Jordan",
                PricePerHour = determinedPrice,
                ConnectorType = slot.Type == ChargerType.FastDC ? $"Fast DC ({slot.PowerOutputKw}kW)" : $"Normal AC ({slot.PowerOutputKw}kW)",
                PowerOutputKw = slot.PowerOutputKw,
                PricePerKwh = slot.PricePerKwh
            };

            ViewBag.StripePublishableKey = _configuration["Stripe:PublishableKey"];

            return View(viewModel);
        }
        // Ensure your Stripe Secret Key is initialized in Program.cs or configured here
        // StripeConfiguration.ApiKey = "sk_test_your_secret_key";

        [HttpPost]
        public async Task<IActionResult> CreatePaymentIntent([FromBody] BookingReservationDto model)
        {
            if (model == null || !ModelState.IsValid)
            {
                return BadRequest(new { message = "Invalid reservation details submitted." });
            }

            // 1. Target the slot using SlotId
            var slot = await _context.ChargerSlots
                .FirstOrDefaultAsync(s => s.SlotId == model.ChargerSlotId);

            if (slot == null)
            {
                return NotFound(new { message = "The selected charger slot does not exist." });
            }

            // 2. Retrieve secure price rate and power output from the database slot
            decimal pricePerKwh = slot.PricePerKwh;
            double powerOutputKw = slot.PowerOutputKw;

            // 3. Parse StartTime safely from string properties
            if (!TimeSpan.TryParse(model.StartTime, out TimeSpan startTime))
            {
                return BadRequest(new { message = "Invalid start time format. Please select valid hours." });
            }

            // Combine Date and Time into precise DateTime objects
            DateTime targetStartDateTime = model.BookingDate.Date + startTime;

            // Validate start time is in the future (allowing a small 5-min drift)
            if (targetStartDateTime < DateTime.Now.AddMinutes(-5))
            {
                return BadRequest(new { message = "The reservation start time cannot be in the past." });
            }

            // 4. Calculate dynamic charging duration and energy needed
            if (model.CurrentChargePct >= model.TargetChargePct)
            {
                return BadRequest(new { message = "Current battery percentage must be strictly less than the target percentage." });
            }

            double energyNeededKwh = model.BatteryCapacityKwh * (model.TargetChargePct - model.CurrentChargePct) / 100.0;

            // Charging efficiency factor: AC ~ 90%, DC ~ 95%
            double efficiency = slot.Type == ChargerType.FastDC ? 0.95 : 0.90;
            double durationHours = energyNeededKwh / (powerOutputKw * efficiency);

            // Calculate precise charging EndTime based on dynamic charging duration
            TimeSpan calculatedDuration = TimeSpan.FromHours(durationHours);
            DateTime targetEndDateTime = targetStartDateTime.Add(calculatedDuration);

            // 5. Overlap Check (Optimized to catch ALL overlap scenarios including nested bookings)
            bool isOverlapping = await _context.Bookings.AnyAsync(b =>
                b.SlotId == slot.SlotId &&
                b.Status != BookingStatus.Cancelled &&
                targetStartDateTime < b.EndTime &&  // New booking starts before an existing one ends
                targetEndDateTime > b.StartTime);   // AND New booking ends after an existing one starts

            if (isOverlapping)
            {
                return BadRequest(new { message = "This exact time window is already fully booked or pending payment." });
            }

            // 6. Calculate secure payment totals for Stripe
            decimal totalAmountJod = (decimal)energyNeededKwh * pricePerKwh;

            // 7. DB Lock Entry tracking all dynamic EV charging fields
            var temporaryBooking = new Booking
            {
                SlotId = slot.SlotId,
                UserId = _userManager.GetUserId(User) ?? "GuestUser",
                StartTime = targetStartDateTime,
                EndTime = targetEndDateTime,
                Status = BookingStatus.Pending,
                BatteryCapacityKwh = model.BatteryCapacityKwh,
                StartingPercentage = model.CurrentChargePct,
                TargetPercentage = model.TargetChargePct,
                EnergyRequestedKwh = energyNeededKwh,
                RatePricePerKwh = pricePerKwh
            };

            _context.Bookings.Add(temporaryBooking);
            await _context.SaveChangesAsync();

            // 8. Initialize Stripe transaction
            try
            {
                var options = new PaymentIntentCreateOptions
                {
                    Amount = (long)(totalAmountJod * 100), // Converted to cents/piasters
                    Currency = "usd",
                    PaymentMethodTypes = new List<string> { "card" },
                    Metadata = new Dictionary<string, string>
                    {
                        { "BookingId", temporaryBooking.BookingId.ToString() }
                    }
                };

                var secretKey = _configuration["Stripe:SecretKey"];
                var stripeClient = new StripeClient(secretKey);
                var service = new PaymentIntentService(stripeClient);
                PaymentIntent intent = await service.CreateAsync(options);

                return Json(new
                {
                    clientSecret = intent.ClientSecret,
                    amount = totalAmountJod,
                    bookingId = temporaryBooking.BookingId
                });
            }
            catch (Exception ex)
            {
                // CRITICAL CLEANUP: If Stripe fails (e.g. invalid API key, network error), 
                // remove the pending booking to avoid locking the slot forever!
                _context.Bookings.Remove(temporaryBooking);
                await _context.SaveChangesAsync();

                return BadRequest(new { message = $"Payment Gateway error: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> ConfirmPayment([FromBody] ConfirmPaymentRequest request)
        {
            if (request == null || request.BookingId <= 0 || string.IsNullOrEmpty(request.PaymentIntentId))
            {
                return BadRequest(new { message = "Invalid payment confirmation details." });
            }

            var booking = await _context.Bookings
                .Include(b => b.ChargerSlot)
                .FirstOrDefaultAsync(b => b.BookingId == request.BookingId);

            if (booking == null)
            {
                return NotFound(new { message = "Booking record not found." });
            }

            if (booking.Status != BookingStatus.Pending && booking.Status != BookingStatus.Confirmed)
            {
                return BadRequest(new { message = "Booking is not in a payable state." });
            }

            booking.Status = BookingStatus.Confirmed;

            var existingPayment = await _context.Payments
                .FirstOrDefaultAsync(p => p.BookingId == request.BookingId);

            if (existingPayment == null)
            {
                decimal amount = (decimal)booking.EnergyRequestedKwh * booking.RatePricePerKwh;

                var payment = new Payment
                {
                    BookingId = booking.BookingId,
                    Amount = amount,
                    StripePaymentIntentId = request.PaymentIntentId,
                    Status = "Succeeded",
                    PaymentDate = DateTime.UtcNow
                };

                _context.Payments.Add(payment);
            }
            else
            {
                existingPayment.StripePaymentIntentId = request.PaymentIntentId;
                existingPayment.Status = "Succeeded";
                existingPayment.PaymentDate = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Booking successfully confirmed and payment completed!" });
        }
    }

    public class ConfirmPaymentRequest
    {
        public int BookingId { get; set; }
        public string PaymentIntentId { get; set; } = string.Empty;
    }
}
