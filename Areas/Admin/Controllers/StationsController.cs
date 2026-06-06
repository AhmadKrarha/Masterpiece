using Materpiece.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Materpiece.Areas.Admin.ViewModels;
using Microsoft.AspNetCore.Identity;
using Materpiece.Models;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.Rendering;


namespace Materpiece.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize] // Restricts access to logged-in users
    public class StationsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager; 

        public StationsController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager; 
        }

        // GET: /Admin/Stations
        public async Task<IActionResult> Index()
        {
            // Get the logged-in User's ID (OwnerId)
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            bool isAdmin = User.IsInRole("Admin");
            bool isOwner = await _context.Stations.AnyAsync(s => s.OwnerId == userId);

            // Access Gate: If they are neither Admin nor Station Owner, block them!
            if (!isAdmin && !isOwner)
            {
                return RedirectToAction("AccessDenied", "Home", new { area = "" });
            }

            // Owners only see their own stations; Admins can see everything
            if (isAdmin)
            {
                var allStations = await _context.Stations.ToListAsync();
                return View(allStations);
            }

            var ownerStations = await _context.Stations
                .Where(s => s.OwnerId == userId)
                .ToListAsync();

            return View(ownerStations);
        }

        // GET: /Admin/Stations/Create
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create()
        {
            var users = await _userManager.Users.ToListAsync();
            var model = new CreateStationViewModel
            {
                UsersList = new SelectList(users, "Id", "Email")
            };
            return View(model);
        }

        // POST: /Admin/Stations/Create
        
        // GET: /Admin/Stations/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var station = await _context.Stations.FindAsync(id);
            if (station == null)
            {
                return NotFound();
            }

            // Security check: Ensure owners can only edit their own stations
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!User.IsInRole("Admin") && station.OwnerId != userId)
            {
                return RedirectToAction("AccessDenied", "Home", new { area = "" });
            }

            return View(station);
        }

        // POST: /Admin/Stations/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Station station)
        {
            if (id != station.StationId)
            {
                return NotFound();
            }

            // Security check re-verification
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var existingStation = await _context.Stations.AsNoTracking().FirstOrDefaultAsync(s => s.StationId == id);

            if (existingStation == null)
            {
                return NotFound();
            }
            if (!User.IsInRole("Admin") && existingStation.OwnerId != userId)
            {
                return RedirectToAction("AccessDenied", "Home", new { area = "" });
            }

            // Reassign correct owner to prevent tampering
            station.OwnerId = existingStation.OwnerId;
            ModelState.Remove("OwnerId");

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(station);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Stations.Any(e => e.StationId == station.StationId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(station);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")] // Or your specific Station Owner role
        public async Task<IActionResult> Create(CreateStationViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var usersForValidation = await _userManager.Users.ToListAsync();
                model.UsersList = new SelectList(usersForValidation, "Id", "Email");
                return View(model);
            }

            // Get the logged-in user's ID (Station Owner)
            var currentUserId = _userManager.GetUserId(User);

            // 1. Initialize EF Core Transaction Execution Strategy
            var strategy = _context.Database.CreateExecutionStrategy();

            try
            {
                await strategy.ExecuteAsync(async () =>
                {
                    using (var transaction = await _context.Database.BeginTransactionAsync())
                    {
                        // 2. Map and Save the New Station
                        var station = new Station
                        {
                            Name = model.Name,
                            Address = model.Address,
                            Latitude = model.Latitude,
                            Longitude = model.Longitude,
                            Description = model.Description,
                            OwnerId = model.SelectedOwnerId,
                            TotalSlots = model.NormalAcSlotsCount + model.FastDcSlotsCount
                        };

                        _context.Stations.Add(station);
                        await _context.SaveChangesAsync(); // Generates the StationId required for FK relationships

                        // 3. Automated Charger Slot Generation Loop
                        int slotNumberCounter = 1;

                        // Generate Normal AC Slots
                        for (int i = 0; i < model.NormalAcSlotsCount; i++)
                        {
                            _context.ChargerSlots.Add(new ChargerSlot
                            {
                                StationId = station.StationId,
                                SlotNumber = $"SLOT-{slotNumberCounter++:D2}", // Outputs: SLOT-01, SLOT-02
                                Type = ChargerType.NormalAC,
                                Status = SlotStatus.Available
                            });
                        }

                        // Generate Fast DC Slots
                        for (int i = 0; i < model.FastDcSlotsCount; i++)
                        {
                            _context.ChargerSlots.Add(new ChargerSlot
                            {
                                StationId = station.StationId,
                                SlotNumber = $"SLOT-{slotNumberCounter++:D2}",
                                Type = ChargerType.FastDC,
                                Status = SlotStatus.Available
                            });
                        }

                        // 4. Commit everything to the database atomically
                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();
                    }
                });

                TempData["Success"] = "Station and charger slots generated successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                // Log the exception details here
                ModelState.AddModelError("", "An error occurred while creating the station and generating slots. Please try again.");
                var usersForError = await _userManager.Users.ToListAsync();
                model.UsersList = new SelectList(usersForError, "Id", "Email");
                return View(model);
            }
        }
        [HttpGet]
        [AllowAnonymous] // Anyone can view details before logging in to book
        public async Task<IActionResult> Details(int id)
        {
            var station = await _context.Stations
                .Include(s => s.ChargerSlots)
                .Include(s => s.Reviews)
                    .ThenInclude(r => r.UserId) // Show real names on reviews to build trust
                .FirstOrDefaultAsync(s => s.StationId == id);

            if (station == null)
            {
                return NotFound();
            }

            var now = DateTime.Now;

            // Calculate real-time aggregate stats for the UX display (subtracting active bookings)
            ViewBag.AverageRating = station.Reviews.Any() ? station.Reviews.Average(r => r.Rating) : 0.0;
            
            ViewBag.AvailableAc = station.ChargerSlots.Count(s => 
                s.Type == ChargerType.NormalAC && 
                s.Status == SlotStatus.Available &&
                !_context.Bookings.Any(b => 
                    b.SlotId == s.SlotId && 
                    (b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.Pending) &&
                    now >= b.StartTime && 
                    now <= b.EndTime)
            );
            
            ViewBag.AvailableDc = station.ChargerSlots.Count(s => 
                s.Type == ChargerType.FastDC && 
                s.Status == SlotStatus.Available &&
                !_context.Bookings.Any(b => 
                    b.SlotId == s.SlotId && 
                    (b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.Pending) &&
                    now >= b.StartTime && 
                    now <= b.EndTime)
            );

            return View(station);
        }
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetSidebarDetails(int id)
        {
            // Fetch the station and its slots directly
            var station = await _context.Stations
                .Include(s => s.ChargerSlots)
                .Include(s => s.Reviews)
                .FirstOrDefaultAsync(s => s.StationId == id);

            if (station == null)
            {
                return NotFound();
            }

            var now = DateTime.Now;
            var slotStatuses = new Dictionary<int, string>();

            foreach (var slot in station.ChargerSlots)
            {
                if (slot.Status == SlotStatus.Maintenance)
                {
                    slotStatuses[slot.SlotId] = "Maintenance";
                    continue;
                }

                // Check if there is an active booking right now (Confirmed or Pending)
                var activeBooking = await _context.Bookings
                    .FirstOrDefaultAsync(b => b.SlotId == slot.SlotId &&
                                              (b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.Pending) &&
                                              now >= b.StartTime && now <= b.EndTime);

                if (activeBooking != null)
                {
                    if (activeBooking.Status == BookingStatus.Confirmed)
                    {
                        slotStatuses[slot.SlotId] = "Reserved";
                    }
                    else if (activeBooking.Status == BookingStatus.Pending)
                    {
                        slotStatuses[slot.SlotId] = "Pending";
                    }
                }
                else
                {
                    slotStatuses[slot.SlotId] = slot.Status == SlotStatus.Occupied ? "Occupied" : "Available";
                }
            }

            ViewBag.SlotStatuses = slotStatuses;

            // Prepare Review User Names mapping
            var reviewerIds = station.Reviews.Select(r => r.UserId).Distinct().ToList();
            var reviewerNames = new Dictionary<string, string>();
            foreach (var rId in reviewerIds)
            {
                var user = await _userManager.FindByIdAsync(rId);
                reviewerNames[rId] = user != null && !string.IsNullOrEmpty(user.Email) ? user.Email.Split('@')[0] : "EV Driver";
            }
            ViewBag.ReviewerNames = reviewerNames;

            // Pass the core Station model straight to the partial view
            return PartialView("_StationSidebarPartial", station);
        }

        // POST: /Admin/Stations/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var station = await _context.Stations.FirstOrDefaultAsync(s => s.StationId == id);
            if (station == null)
            {
                return Json(new { success = false, message = "Station not found." });
            }

            // Security check: only Admin or owner can delete
            if (!User.IsInRole("Admin") && station.OwnerId != userId)
            {
                return Json(new { success = false, message = "You do not have permission to delete this station." });
            }

            var strategy = _context.Database.CreateExecutionStrategy();
            bool isDeleted = false;

            await strategy.ExecuteAsync(async () =>
            {
                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    try
                    {
                        // 1. Delete Reviews associated with this station
                        var reviews = await _context.Reviews.Where(r => r.StationId == id).ToListAsync();
                        _context.Reviews.RemoveRange(reviews);

                        // 2. Delete Payments & Bookings associated with this station's slots
                        var slots = await _context.ChargerSlots.Where(cs => cs.StationId == id).ToListAsync();
                        var slotIds = slots.Select(cs => cs.SlotId).ToList();

                        var bookings = await _context.Bookings.Where(b => slotIds.Contains(b.SlotId)).ToListAsync();
                        var bookingIds = bookings.Select(b => b.BookingId).ToList();

                        var payments = await _context.Payments.Where(p => bookingIds.Contains(p.BookingId)).ToListAsync();
                        
                        _context.Payments.RemoveRange(payments);
                        _context.Bookings.RemoveRange(bookings);
                        _context.ChargerSlots.RemoveRange(slots);
                        _context.Stations.Remove(station);

                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();
                        isDeleted = true;
                    }
                    catch (Exception)
                    {
                        await transaction.RollbackAsync();
                    }
                }
            });

            if (isDeleted)
            {
                return Json(new { success = true, message = "Station and all associated charging slots deleted successfully!" });
            }

            return Json(new { success = false, message = "An error occurred while deleting the station." });
        }
    }
}

