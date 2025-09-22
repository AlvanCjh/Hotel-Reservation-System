using System.Threading.Tasks;

namespace Demo.Services
{
    public interface IEmailSender
    {
        Task<bool> SendNewsletterWelcomeAsync(string toEmail);
    }
}
