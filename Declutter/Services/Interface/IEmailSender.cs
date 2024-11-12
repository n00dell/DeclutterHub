namespace DeclutterHub.Services.Interface
{
    public interface IEmailSender
    {
        Task SendEmailAsync(string toEmail, string subject, string body);
    }

}
