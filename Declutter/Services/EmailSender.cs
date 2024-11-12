using DeclutterHub.Services.Interface;
using Microsoft.Extensions.Options;
using Mailjet.Client;
using Mailjet.Client.Resources;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using DeclutterHub.Models;

namespace DeclutterHub.Services
{
    public class EmailSender : IEmailSender
    {

        private readonly MailjetSettings _settings;

        public EmailSender(IOptions<MailjetSettings> options)
        {
            _settings = options.Value;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            var client = new MailjetClient(_settings.ApiKey, _settings.SecretKey);

            var request = new MailjetRequest
            {
                Resource = Send.Resource
            }
            .Property(Send.FromEmail, _settings.FromEmail)
            .Property(Send.FromName, _settings.DisplayName)
            .Property(Send.Subject, subject)
            .Property(Send.HtmlPart, body)
            .Property(Send.Recipients, new JArray
            {
                new JObject
                {
                    {"Email", toEmail}
                }
            });

            var response = await client.PostAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Failed to send email. StatusCode: {response.StatusCode}, Error: {response.GetErrorMessage()}");
            }
        }
    }
}

