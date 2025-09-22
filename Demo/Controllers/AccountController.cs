using Demo.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Text;

namespace Demo.Controllers
{
    [Route("Account")]
    public class AccountController : Controller
    {
        private readonly DB _context;
        private readonly IConfiguration _configuration; // field

        private string HashPassword(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                StringBuilder builder = new StringBuilder();
                foreach (var b in bytes)
                {
                    builder.Append(b.ToString("x2")); // Convert byte to hex string
                }
                return builder.ToString();
            }
        }


        public AccountController(DB context)
        {
            _context = context;
        }

        private async Task<bool> IsReCaptchaValid()
        {
            var captchaResponse = Request.Form["g-recaptcha-response"];
            var secretKey = _configuration["GoogleReCaptcha:SecretKey"];

            using var httpClient = new HttpClient();
            var result = await httpClient.GetStringAsync(
                $"https://www.google.com/recaptcha/api/siteverify?secret={secretKey}&response={captchaResponse}"
            );

            var captchaResult = JsonSerializer.Deserialize<ReCaptchaResponse>(result);
            return captchaResult.Success;
        }

        public class ReCaptchaResponse
        {
            public bool Success { get; set; }
            public DateTime Challenge_ts { get; set; }
            public string Hostname { get; set; }
        }

        // === Sign Up === //

        [HttpGet("SignUp")]
        public IActionResult SignUp() => View();

        [HttpPost("SignUp")]
        public IActionResult SignUp(User user)
        {
            if (_context.Users.Any(u => u.Email == user.Email))
            {
                ModelState.AddModelError("Error", "Email already registered.");
                return View(user);
            }

            // Assign default profile picture and role
            user.PhotoURL = "default-profile.png";
            user.Role = "member"; //

            // Hashing password
            user.Password = HashPassword(user.Password);

            _context.Users.Add(user);
            _context.SaveChanges();

            return RedirectToAction("MemberLogin");
        }



        // === Log In === //

        [HttpGet("MemberLogin")]
        public IActionResult MemberLogin()
        {
            return View();
        }

        [HttpPost("MemberLogin")]
        public async Task<IActionResult> MemberLogin(string email, string password)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                ViewBag.Error = "Invalid member credentials";
                return View("MemberLogin");
            }

            var user = _context.Users.FirstOrDefault(u => u.Email == email);

            // Do not reveal whether user exists — generic message, but we still need to handle lockout for existing user.
            if (user == null || user.Role != "member")
            {
                ViewBag.Error = "Invalid member credentials";
                return View("MemberLogin");
            }

            // If locked out
            if (user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTime.UtcNow)
            {
                var remainingSeconds = (int)(user.LockoutEnd.Value - DateTime.UtcNow).TotalSeconds;
                ViewBag.Error = $"Account locked. Try again in {remainingSeconds} seconds.";
                ViewBag.LockoutSeconds = remainingSeconds;
                return View("MemberLogin");
            }

            var hashedPassword = HashPassword(password);

            // Successful login
            if (user.Password == hashedPassword)
            {
                user.FailedLoginAttempts = 0;
                user.LockoutEnd = null;
                await _context.SaveChangesAsync();

                // Create claims and sign in as before
                var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, user.Email),
            new Claim("FullName", user.Name),
            new Claim(ClaimTypes.Role, user.Role)
        };
                var claimsIdentity = new ClaimsIdentity(claims, "MyCookieAuth");
                await HttpContext.SignInAsync("MyCookieAuth", new ClaimsPrincipal(claimsIdentity),
                    new AuthenticationProperties { IsPersistent = true });

                return RedirectToAction("Index", "Home");
            }

            // Failed login attempt: increment
            user.FailedLoginAttempts += 1;

            if (user.FailedLoginAttempts >= 3)
            {
                // Lock account for 1 minute
                user.LockoutEnd = DateTime.UtcNow.AddMinutes(1);
                user.FailedLoginAttempts = 0; // reset attempts after lock
                await _context.SaveChangesAsync();

                ViewBag.Error = "Account locked for 1 minute due to multiple failed attempts.";
                ViewBag.LockoutSeconds = 60;
                return View("MemberLogin");
            }
            else
            {
                int remaining = 3 - user.FailedLoginAttempts;
                await _context.SaveChangesAsync();
                ViewBag.Error = $"Invalid member credentials. {remaining} attempt(s) remaining.";
                return View("MemberLogin");
            }
        }

        // === Admin Login ===//

        [HttpGet("AdminLogin")]
        public IActionResult AdminLogin()
        {
            return View();
        }

        [HttpPost("AdminLogin")]
        public async Task<IActionResult> AdminLogin(string email, string password)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                ViewBag.Error = "Invalid admin credentials";
                return View("AdminLogin");
            }

            var user = _context.Users.FirstOrDefault(u => u.Email == email);

            if (user == null || user.Role != "admin")
            {
                ViewBag.Error = "Invalid admin credentials";
                return View("AdminLogin");
            }

            if (user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTime.UtcNow)
            {
                var remaining = (int)(user.LockoutEnd.Value - DateTime.UtcNow).TotalSeconds;
                ViewBag.Error = $"Account locked. Try again in {remaining} seconds.";
                ViewBag.LockoutSeconds = remaining;
                return View("AdminLogin");
            }

            var hashedPassword = HashPassword(password);

            if (user.Password == hashedPassword)
            {
                user.FailedLoginAttempts = 0;
                user.LockoutEnd = null;
                await _context.SaveChangesAsync();

                var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, user.Email),
            new Claim("FullName", user.Name),
            new Claim(ClaimTypes.Role, user.Role)
        };
                var claimsIdentity = new ClaimsIdentity(claims, "MyCookieAuth");
                await HttpContext.SignInAsync("MyCookieAuth", new ClaimsPrincipal(claimsIdentity));

                return RedirectToAction("Dashboard", "Admin");
            }

            user.FailedLoginAttempts += 1;
            if (user.FailedLoginAttempts >= 3)
            {
                user.LockoutEnd = DateTime.UtcNow.AddMinutes(1);
                user.FailedLoginAttempts = 0;
                await _context.SaveChangesAsync();
                ViewBag.Error = "Account locked for 1 minute due to multiple failed attempts.";
                ViewBag.LockoutSeconds = 60;
                return View("AdminLogin");
            }
            else
            {
                int remaining = 3 - user.FailedLoginAttempts;
                await _context.SaveChangesAsync();
                ViewBag.Error = $"Invalid admin credentials. {remaining} attempt(s) remaining.";
                return View("AdminLogin");
            }
        }

        // === Log Out === //

        [HttpGet("Logout")]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync("MyCookieAuth");
            return RedirectToAction("Index", "Home");
        }

        // === Profile === //

        [Authorize]
        [HttpGet("Profile")]
        public IActionResult Profile()
        {
            var email = User.Identity.Name;
            var user = _context.Users.FirstOrDefault(u => u.Email == email);

            if (user == null)
            {
                return RedirectToAction("Logout");
            }

            return View(user);
        }

        [Authorize]
        [HttpPost("Profile")]
        public async Task<IActionResult> Profile(User updatedUser, IFormFile photoFile, string croppedImageData)
        {
            var email = User.Identity.Name;
            var existingUser = _context.Users.FirstOrDefault(u => u.Email == email);

            if (existingUser == null)
            {
                return RedirectToAction("Logout");
            }

            // Update allowed fields
            existingUser.Name = updatedUser.Name;

            // Update password if provided
            if (!string.IsNullOrEmpty(updatedUser.Password))
            {
                existingUser.Password = HashPassword(updatedUser.Password);
            }

            // Handle profile picture update
            if (!string.IsNullOrEmpty(croppedImageData) || photoFile != null)
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                string uniqueFileName;

                // Prioritize the cropped image if available
                if (!string.IsNullOrEmpty(croppedImageData))
                {
                    // Process the base64 cropped image
                    var base64Data = croppedImageData.Split(',')[1];
                    var bytes = Convert.FromBase64String(base64Data);
                    uniqueFileName = Guid.NewGuid().ToString() + ".jpg";
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    await System.IO.File.WriteAllBytesAsync(filePath, bytes);
                }
                else if (photoFile != null && photoFile.Length > 0)
                {
                    // Fallback to original file if no cropping was done
                    uniqueFileName = Guid.NewGuid().ToString() + "_" + photoFile.FileName;
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await photoFile.CopyToAsync(fileStream);
                    }
                }
                else
                {
                    // No image provided
                    return View(existingUser);
                }

                // Delete old profile picture if it's not the default one
                if (existingUser.PhotoURL != "default-profile.png")
                {
                    var oldFilePath = Path.Combine(uploadsFolder, existingUser.PhotoURL);
                    if (System.IO.File.Exists(oldFilePath))
                    {
                        System.IO.File.Delete(oldFilePath);
                    }
                }

                existingUser.PhotoURL = uniqueFileName;
            }

            _context.SaveChanges();

            return RedirectToAction("Profile");
        }
    }


}