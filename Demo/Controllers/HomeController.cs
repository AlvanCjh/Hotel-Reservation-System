using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Net.Mail;

namespace Demo.Controllers;

public class HomeController : Controller
{
    // TODO
    public HomeController()
    {
        
    }

    // GET: Home/Index
    public IActionResult Index()
    {
        return View();
    }

    public IActionResult About()
    {
        return View();
    }

    public IActionResult Dining()
    {
        return View();
    }

    public IActionResult Experience()
    {
        return View();
    }

    // Newsletter
    [HttpPost]
    public async Task<IActionResult> Subscribe(SubscribeViewModel model)
    {
        if (ModelState.IsValid)
        {
            // Replace with your email & SMTP info
            var fromAddress = new MailAddress("youremail@example.com", "Your Website");
            var toAddress = new MailAddress(model.Email);
            const string fromPassword = "YOUR_EMAIL_PASSWORD"; // use secrets instead in production
            const string subject = "Subscription Confirmation";
            string body = "Thank you for subscribing to our newsletter!";

            var smtp = new SmtpClient
            {
                Host = "smtp.gmail.com", // change to your provider's SMTP
                Port = 587,
                EnableSsl = true,
                Credentials = new NetworkCredential(fromAddress.Address, fromPassword)
            };

            using (var message = new MailMessage(fromAddress, toAddress)
            {
                Subject = subject,
                Body = body
            })
            {
                await smtp.SendMailAsync(message);
            }

            TempData["Message"] = "Subscription successful! Please check your email.";
        }

        return RedirectToAction("Index");
    }

    public class SubscribeViewModel
    {
        public string Email { get; set; }
    }

}
