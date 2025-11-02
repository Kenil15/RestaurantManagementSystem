using System.Threading.Tasks;

namespace ForUpworkRestaurentManagement.Services
{
    public interface IEmailService
    {
        Task SendAsync(string toEmail, string subject, string htmlBody);
    }
}
