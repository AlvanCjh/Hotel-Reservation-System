using Demo.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Demo.Controllers
{
    public class ReservationsController : Controller
    {
        private readonly DB _context;
        private readonly ILogger<ReservationsController> _logger;

        public ReservationsController(DB context, ILogger<ReservationsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ✅ GET: /Reservations
        public async Task<IActionResult> Index(DateTime? checkIn, DateTime? checkOut)
        {
            var today = DateOnly.FromDateTime(DateTime.Today);

            // ✅ Pass dates to ViewBag so Razor can use them
            ViewBag.CheckIn = checkIn?.ToString("yyyy-MM-dd");
            ViewBag.CheckOut = checkOut?.ToString("yyyy-MM-dd");

            var rooms = await _context.Rooms
                .Include(r => r.RoomType)
                .Include(r => r.RoomImages)
                .Include(r => r.Reservations)
                .ToListAsync();

            var model = rooms.Select(r => new RoomAvailabilityViewModel
            {
                RoomId = r.RoomId,
                TypeName = r.RoomType?.Name ?? "(unknown)",
                Price = r.RoomType?.Price ?? 0m,
                Description = r.Description,
                RoomImage = r.RoomImages.FirstOrDefault()?.ImageUrl,
                ImageUrls = r.RoomImages.Select(img => img.ImageUrl).ToList(),

                CurrentReservation = r.Reservations
                    .OrderByDescending(res => res.CheckIn)
                    .FirstOrDefault(res =>
                        (res.Status == ReservationStatus.CheckedIn) ||
                        (res.Status == ReservationStatus.Pending && res.IsActive(today))
                    ),

                HasActiveReservation = r.Reservations.Any(res =>
                    (res.Status == ReservationStatus.CheckedIn) ||
                    (res.Status == ReservationStatus.Pending && res.IsActive(today))
                )
            }).ToList();

            // ✅ Filter by selected dates (only show rooms not booked in range)
            if (checkIn.HasValue && checkOut.HasValue)
            {
                var checkInDO = DateOnly.FromDateTime(checkIn.Value);
                var checkOutDO = DateOnly.FromDateTime(checkOut.Value);

                model = model.Where(room =>
                    !_context.Reservations.Any(res =>
                        res.RoomId == room.RoomId &&
                        res.Status != ReservationStatus.Cancelled &&
                        res.Status != ReservationStatus.CheckedOut &&
                        !(checkOutDO <= res.CheckIn || checkInDO >= res.CheckOut)
                    )
                ).ToList();
            }

            return View(model);
        }

        // GET: /Reservations/Create?roomId=R01
        public async Task<IActionResult> Create(string roomId, DateTime? checkIn, DateTime? checkOut)
        {
            try
            {
                if (string.IsNullOrEmpty(roomId))
                {
                    var rooms = await _context.Rooms
                        .Include(r => r.RoomType)
                        .AsNoTracking()
                        .ToListAsync();

                    return View("SelectRoom", rooms);
                }

                var room = await _context.Rooms
                    .AsNoTracking()
                    .Include(r => r.RoomType)
                    .FirstOrDefaultAsync(r => r.RoomId == roomId);

                if (room == null)
                {
                    _logger.LogWarning($"Room {roomId} not found");
                    return NotFound();
                }

                if (room.RoomType == null)
                {
                    _logger.LogWarning($"Room {roomId} has no associated RoomType");
                    return NotFound("Room type information missing");
                }

                var vm = new CreateReservationViewModel
                {
                    RoomId = room.RoomId,
                    TypeId = room.TypeId,
                    RoomTypeName = room.RoomType.Name,
                    Price = room.RoomType.Price,
                    CheckInDate = checkIn ?? DateTime.Today,
                    CheckOutDate = checkOut ?? DateTime.Today.AddDays(1)
                };

                return View(vm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading reservation form");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateReservationViewModel vm)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var dbRoom = await _context.Rooms
                        .Include(r => r.RoomType)
                        .FirstOrDefaultAsync(r => r.RoomId == vm.RoomId);

                    if (dbRoom != null && dbRoom.RoomType != null)
                    {
                        vm.TypeId = dbRoom.TypeId;
                        vm.RoomTypeName = dbRoom.RoomType.Name;
                        vm.Price = dbRoom.RoomType.Price;
                    }

                    return View(vm);
                }

                await using var transaction = await _context.Database.BeginTransactionAsync();

                var room = await _context.Rooms
                    .Include(r => r.RoomType)
                    .FirstOrDefaultAsync(r => r.RoomId == vm.RoomId);

                if (room == null)
                {
                    ModelState.AddModelError("", "Room not found.");
                    return View(vm);
                }

                var checkInDO = DateOnly.FromDateTime(vm.CheckInDate);
                var checkOutDO = DateOnly.FromDateTime(vm.CheckOutDate);

                if (checkOutDO <= checkInDO)
                {
                    ModelState.AddModelError("", "Check-out must be after check-in.");
                    return View(vm);
                }

                bool isAvailable = !await _context.Reservations
                    .Where(r => r.RoomId == vm.RoomId &&
                                r.Status != ReservationStatus.Cancelled &&
                                r.Status != ReservationStatus.CheckedOut)
                    .AnyAsync(r => !(checkOutDO <= r.CheckIn || checkInDO >= r.CheckOut));

                if (!isAvailable)
                {
                    ModelState.AddModelError("", "Room is not available for the selected dates.");
                    return View(vm);
                }

                var reservation = new Reservation
                {
                    ReservationId = Guid.NewGuid().ToString(),
                    RoomId = vm.RoomId,
                    Email = User?.Identity?.Name,
                    CheckIn = checkInDO,
                    CheckOut = checkOutDO,
                    TotalPrice = (room.RoomType?.Price ?? 0m) * (checkOutDO.DayNumber - checkInDO.DayNumber),
                    Status = ReservationStatus.Pending
                };

                _context.Reservations.Add(reservation);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return RedirectToAction(nameof(Confirmation), new { reservationId = reservation.ReservationId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating reservation");
                ModelState.AddModelError("", "An error occurred while creating the reservation.");
                return View(vm);
            }
        }

        [HttpPost]
        public async Task<IActionResult> CheckIn(string id)
        {
            if (string.IsNullOrEmpty(id)) return BadRequest();

            var reservation = await _context.Reservations.FindAsync(id);
            if (reservation == null) return NotFound();

            reservation.Status = ReservationStatus.CheckedIn;
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> CheckOut(string id)
        {
            if (string.IsNullOrEmpty(id)) return BadRequest();

            var reservation = await _context.Reservations.FindAsync(id);
            if (reservation == null) return NotFound();

            reservation.Status = ReservationStatus.CheckedOut;
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Confirmation(string reservationId)
        {
            if (string.IsNullOrEmpty(reservationId)) return RedirectToAction(nameof(Index));

            var reservation = await _context.Reservations
                .Include(r => r.Room)
                    .ThenInclude(rt => rt.RoomType)
                .FirstOrDefaultAsync(r => r.ReservationId == reservationId);

            if (reservation == null) return NotFound();

            return View(reservation);
        }

        public IActionResult Receipt(string id)
        {
            var reservation = _context.Reservations
                .Include(r => r.Room)
                    .ThenInclude(rt => rt.RoomType)
                .Include(r => r.User)
                .FirstOrDefault(r => r.ReservationId == id);

            if (reservation == null) return NotFound();

            return View(reservation);
        }

        public async Task<IActionResult> MyBookings()
        {
            if (User?.Identity?.IsAuthenticated != true)
                return RedirectToAction("MemberLogin", "Account");

            var email = User.Identity.Name;
            var today = DateOnly.FromDateTime(DateTime.Today);

            var reservations = await _context.Reservations
                .Include(r => r.Room)
                    .ThenInclude(rt => rt.RoomType)
                .Where(r => r.Email == email)
                .OrderByDescending(r => r.CheckIn)
                .ToListAsync();

            var upcoming = reservations
                .Where(r => r.CheckIn >= today && r.Status != ReservationStatus.Cancelled)
                .ToList();

            var past = reservations
                .Where(r => r.CheckOut < today || r.Status == ReservationStatus.CheckedOut || r.Status == ReservationStatus.Cancelled)
                .ToList();

            var model = new MyBookingsViewModel
            {
                UpcomingReservations = upcoming.Select(r => new ReservationListItemViewModel
                {
                    ReservationId = r.ReservationId,
                    UserEmail = r.Email,
                    RoomId = r.RoomId,
                    RoomTypeName = r.Room?.RoomType?.Name,
                    CheckIn = r.CheckIn,
                    CheckOut = r.CheckOut,
                    TotalPrice = r.TotalPrice,
                    Status = r.Status.ToString(),
                    RoomDescription = r.Room?.Description,
                    RoomImage = r.Room?.RoomImages.FirstOrDefault()?.ImageUrl
                }).ToList(),
                PastReservations = past.Select(r => new ReservationListItemViewModel
                {
                    ReservationId = r.ReservationId,
                    UserEmail = r.Email,
                    RoomId = r.RoomId,
                    RoomTypeName = r.Room?.RoomType?.Name,
                    CheckIn = r.CheckIn,
                    CheckOut = r.CheckOut,
                    TotalPrice = r.TotalPrice,
                    Status = r.Status.ToString(),
                    RoomDescription = r.Room?.Description,
                    RoomImage = r.Room?.RoomImages.FirstOrDefault()?.ImageUrl
                }).ToList()
            };

            return View(model);
        }
    }

    // ViewModels
    public class RoomAvailabilityViewModel
    {
        public string RoomId { get; set; }
        public string TypeName { get; set; }
        public decimal Price { get; set; }
        public string Description { get; set; }
        public string RoomImage { get; set; }
        public List<string> ImageUrls { get; set; } = new();
        public bool HasActiveReservation { get; set; }
        public Reservation CurrentReservation { get; set; }
    }

    public class CreateReservationViewModel
    {
        public string RoomId { get; set; }
        public string TypeId { get; set; }
        public string RoomTypeName { get; set; }
        public decimal Price { get; set; }

        [DataType(DataType.Date)]
        public DateTime CheckInDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime CheckOutDate { get; set; }
    }
}
