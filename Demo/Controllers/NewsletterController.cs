using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Demo.Services;

namespace Demo.Controllers
{
    public class NewsletterController : Controller
    {
        private readonly IEmailSender _email;

        public NewsletterController(IEmailSender email)
        {
            _email = email;
        }

        public class SubscribeInput
        {
            [Required, EmailAddress]
            public string Email { get; set; } = string.Empty;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Subscribe([FromForm] SubscribeInput input)
        {
            if (!ModelState.IsValid)
            {
                TempData["Message"] = "Please enter a valid email address.";
                return Redirect(Request.Headers["Referer"].ToString() ?? Url.Action("Index", "Home"));
            }

            try
            { 
            var ok = await _email.SendNewsletterWelcomeAsync(input.Email);

            TempData["Message"] = ok
                ? "Subscription successful! Please check your inbox."
                : "We couldn't send the email right now. Please try again later.";

            // Optional: save input.Email to your DB for future campaigns
            } 
            catch (Exception ex)
            {
                TempData["Message"] = "SendGrid error: " + ex.Message;
            }
            return Redirect(Request.Headers["Referer"].ToString() ?? Url.Action("Index", "Home"));
        }
    }
}
