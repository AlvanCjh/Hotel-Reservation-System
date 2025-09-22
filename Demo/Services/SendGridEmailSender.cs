using Demo.Options;
using Microsoft.Extensions.Options;
using SendGrid;
using SendGrid.Helpers.Mail;
using System.Threading.Tasks;
using Demo.Options;

namespace Demo.Services
{
    public class SendGridEmailSender : IEmailSender
    {
        private readonly SendGridOptions _opts;

        public SendGridEmailSender(IOptions<SendGridOptions> opts)
        {
            _opts = opts.Value;
        }

        public async Task<bool> SendNewsletterWelcomeAsync(string toEmail)
        {
            var client = new SendGridClient(_opts.ApiKey);

            var from = new EmailAddress(_opts.FromEmail, _opts.FromName);
            var to = new EmailAddress(toEmail);

            var subject = "Thanks for subscribing!";
            var plain = $"Hi,\n\nThanks for subscribing to our newsletter. Any news and offer will be updated to you\n\n— {_opts.FromName}";
            var html = $"<p>Hi,</p><p>Thanks for subscribing to our newsletter.</p><p>— {_opts.FromName}</p>";

            var msg = MailHelper.CreateSingleEmail(from, to, subject, plain, html);

            var response = await client.SendEmailAsync(msg);

            if (response.IsSuccessStatusCode)
            {
                return true;
            }
            else
            {
                // Log full response for debugging
                var body = await response.Body.ReadAsStringAsync();
                Console.WriteLine("SendGrid Error: " + body);

                // You could also return the body as part of TempData
                throw new Exception($"SendGrid failed: {response.StatusCode}, {body}");
            }
        }

    }
}
