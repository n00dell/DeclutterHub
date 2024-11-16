using Mailjet.Client;
using Mailjet.Client.Resources;
using Newtonsoft.Json.Linq;

namespace DeclutterHub.Services
{
    public class MailService : IMailService
    {
        private readonly IConfiguration _configuration;

        public MailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            var apiKey = _configuration["MailJet:ApiKey"];
            var secretKey = _configuration["MailJet:SecretKey"];

            if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(secretKey))
            {
                throw new Exception("MailJet API Key or Secret Key is missing in the configuration.");
            }

            var client = new MailjetClient(apiKey, secretKey);

            var request = new MailjetRequest
            {
                Resource = Send.Resource
            }
            .Property(Send.FromEmail, _configuration["MailJet:FromEmail"])
            .Property(Send.FromName, _configuration["MailJet:DisplayName"])
            .Property(Send.Subject, subject)
            .Property(Send.TextPart, body)
            .Property(Send.Recipients, new JArray
            {
            new JObject { { "Email", toEmail } }
            });

            var response = await client.PostAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Failed to send email. StatusCode: {response.StatusCode}, Error: {response.GetErrorMessage()}");
            }
        }
    }
}
