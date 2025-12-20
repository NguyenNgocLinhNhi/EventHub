using System.Threading.Tasks;

namespace EventManagementSystem.Web.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string htmlMessage);
    }
}
