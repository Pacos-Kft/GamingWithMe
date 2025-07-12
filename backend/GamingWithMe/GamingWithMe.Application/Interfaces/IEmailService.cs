using System.Threading.Tasks;

namespace GamingWithMe.Application.Interfaces
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string? link);
    }
}