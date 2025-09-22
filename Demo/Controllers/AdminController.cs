using Demo.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Linq;

namespace Demo.Controllers
{

    [Authorize(Roles = "admin")]
    [Route("Admin")]
    public class AdminController : Controller
    {
        private readonly DB _context;

        public AdminController(DB context)
        {
            _context = context;
        }

        [HttpGet("Dashboard")]
        public IActionResult Dashboard()
        {
            var adminName = User.FindFirst("FullName")?.Value;
            ViewBag.AdminName = adminName;

            return View();
        }

        [HttpGet("Users")]
        public IActionResult UserManage()
        {
            var users = _context.Users
            .Select(u =>  new UserViewModel
            {
                Email = u.Email,
                Name = u.Name,
                PhotoURL = u.PhotoURL,
                Role = u.Role
            })
            .ToList();

            return View(users);
        }

        [HttpGet("UserDetails/{email}")]
        public IActionResult UserDetail(string email)
        {
            var user = _context.Users
                 .Where(u => u.Email == email)
                 .Select(u => new UserViewModel
                 {
                     Email = u.Email,
                     Name = u.Name,
                     PhotoURL = u.PhotoURL,
                     Role = u.Role
                 })
                 .FirstOrDefault();

            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        [HttpGet("Rooms")]
        public async Task<IActionResult> RoomManage()
        {
            var rooms = await _context.Rooms
                .Include(r => r.RoomType)
                .Include(r => r.RoomImages)
                .Select(r => new RoomListItemViewModel
                {
                    RoomId = r.RoomId,
                    TypeId = r.TypeId,
                    TypeName = r.RoomType.Name,
                    Description = r.Description,
                    ImageUrls = r.RoomImages.Select(img => img.ImageUrl).ToList()
                })
                .ToListAsync();

            return View(rooms);
        }


        private async Task PopulateRoomTypeItems(RoomCreateEditViewModel vm)
        {
            vm.RoomTypeItems = await _context.RoomTypes
                .Select(rt => new SelectListItem
                {
                    Value = rt.TypeId,
                    Text = $"{rt.Name} ({rt.TypeId})",
                    Selected = rt.TypeId == vm.TypeId
                })
                .ToListAsync();
        }

        [HttpGet("AddRoom")]
        public async Task<IActionResult> AddRoom()
        {
            var vm = new RoomCreateEditViewModel();
            await PopulateRoomTypeItems(vm);
            return View(vm);
        }

        [HttpPost("AddRoom")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddRoom(RoomCreateEditViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                await PopulateRoomTypeItems(vm);
                return View(vm);
            }

            if (await _context.Rooms.AnyAsync(r => r.RoomId == vm.RoomId))
            {
                ModelState.AddModelError(nameof(vm.RoomId), "A room with this ID already exists.");
                await PopulateRoomTypeItems(vm);
                return View(vm);
            }

            var room = new Room
            {
                RoomId = vm.RoomId,
                TypeId = vm.TypeId,
                Description = vm.Description
            };

            _context.Rooms.Add(room);
            await _context.SaveChangesAsync();

            // Save images (if uploaded)
            if (vm.UploadedImages != null && vm.UploadedImages.Any())
            {
                foreach (var file in vm.UploadedImages)
                {
                    var fileName = $"{Guid.NewGuid()}_{file.FileName}";
                    var path = Path.Combine("wwwroot/images/rooms", fileName);

                    using (var stream = new FileStream(path, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }

                    _context.RoomImages.Add(new RoomImage
                    {
                        RoomId = room.RoomId,
                        ImageUrl = $"/images/rooms/{fileName}"
                    });
                }
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(RoomManage));
        }


        [HttpGet("EditRoom/{roomId}")]
        public async Task<IActionResult> EditRoom(string roomId)
        {
            var room = await _context.Rooms
                .Include(r => r.RoomImages)
                .FirstOrDefaultAsync(r => r.RoomId == roomId);

            if (room == null) return NotFound();

            var vm = new RoomCreateEditViewModel
            {
                RoomId = room.RoomId,
                TypeId = room.TypeId,
                Description = room.Description,
                ExistingImages = room.RoomImages.Select(img => img.ImageUrl).ToList()
            };

            await PopulateRoomTypeItems(vm);
            return View(vm);
        }

        [HttpPost("EditRoom/{roomId}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditRoom(string roomId, RoomCreateEditViewModel vm)
        {
            if (roomId != vm.RoomId) return BadRequest();

            if (!ModelState.IsValid)
            {
                await PopulateRoomTypeItems(vm);
                return View(vm);
            }

            var room = await _context.Rooms
                .Include(r => r.RoomImages)
                .FirstOrDefaultAsync(r => r.RoomId == roomId);

            if (room == null) return NotFound();

            // Update fields
            room.TypeId = vm.TypeId;
            room.Description = vm.Description;

            // Save new uploaded images
            if (vm.UploadedImages != null && vm.UploadedImages.Any())
            {
                foreach (var file in vm.UploadedImages)
                {
                    var fileName = $"{Guid.NewGuid()}_{file.FileName}";
                    var path = Path.Combine("wwwroot/images/rooms", fileName);

                    using (var stream = new FileStream(path, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }

                    _context.RoomImages.Add(new RoomImage
                    {
                        RoomId = room.RoomId,
                        ImageUrl = $"/images/rooms/{fileName}"
                    });
                }
            }

            // ✅ Don’t call Update, just SaveChanges since room is already tracked
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(RoomManage));
        }


        [HttpPost("DeleteRoomImage")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteRoomImage([FromBody] DeleteImageRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.RoomId) || string.IsNullOrEmpty(request.ImageUrl))
                return BadRequest();

            var image = await _context.RoomImages
                .FirstOrDefaultAsync(img => img.RoomId == request.RoomId && img.ImageUrl == request.ImageUrl);

            if (image != null)
            {
                _context.RoomImages.Remove(image);

                // delete physical file
                var filePath = Path.Combine("wwwroot", image.ImageUrl.TrimStart('/'));
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }

                await _context.SaveChangesAsync();
            }

            return Ok();
        }

        public class DeleteImageRequest
        {
            public string RoomId { get; set; }
            public string ImageUrl { get; set; }
        }



        [HttpGet("Reservations")]
        public async Task<IActionResult> ReservationManage(string? userEmail)
        {
            ViewBag.FilterEmail = userEmail ?? "";

            var query = _context.Reservations
                .Include(r => r.User)
                .Include(r => r.Room)
                    .ThenInclude(room => room.RoomType)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(userEmail))
                query = query.Where(r => r.Email == userEmail);

            var list = await query
                .Select(r => new ReservationListItemViewModel
                {
                    ReservationId = r.ReservationId,
                    UserEmail = r.Email,
                    UserName = r.User.Name,
                    RoomId = r.RoomId,
                    RoomTypeName = r.Room.RoomType.Name,
                    CheckIn = r.CheckIn,
                    CheckOut = r.CheckOut,
                    TotalPrice = r.TotalPrice,
                    Status = r.Status.ToString()
                })
                .ToListAsync();

            return View(list);
        }


        [HttpGet("ReservationDetails/{reservationId}")]
        public async Task<IActionResult> ReservationDetails(string reservationId)
        {
            if (string.IsNullOrWhiteSpace(reservationId)) return BadRequest();

            var r = await _context.Reservations
                .Include(res => res.User)
                .Include(res => res.Room)
                    .ThenInclude(room => room.RoomType)
                .FirstOrDefaultAsync(res => res.ReservationId == reservationId);

            if (r == null) return NotFound();

            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var detail = new ReservationDetailViewModel
            {
                ReservationId = r.ReservationId,
                UserEmail = r.Email,
                UserName = r.User.Name,
                RoomId = r.RoomId,
                RoomTypeName = r.Room.RoomType.Name,
                CheckIn = r.CheckIn,
                CheckOut = r.CheckOut,
                TotalPrice = r.TotalPrice,
                Status = r.Status.ToString(),
            };

            return View(detail);
        }


    }
}
