using GamingWithMe.Application.Interfaces;
using GamingWithMe.Domain.Entities;
using Mailjet.Client;
using Mailjet.Client.Resources;
using Mailjet.Client.TransactionalEmails;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;

namespace GamingWithMe.Infrastructure.Services
{
    public class MailjetEmailService : IEmailService
    {
        private readonly MailjetSettings _mailjetSettings;


        public MailjetEmailService(IOptions<MailjetSettings> mailjetSettings)
        {
            _mailjetSettings = mailjetSettings.Value;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string? link)
        {
            var client = new MailjetClient(_mailjetSettings.ApiKey, _mailjetSettings.SecretKey);

            MailjetRequest request = new MailjetRequest
            {
                Resource = SendV31.Resource,
            }
            .Property(Send.Messages, new JArray {
                new JObject {
                    {
                        "From",
                        new JObject {
                            {"Email", _mailjetSettings.SenderEmail}, 
                            {"Name", _mailjetSettings.SenderName}
                        }
                    }, {
                        "To",
                        new JArray {
                            new JObject {
                                {"Email", toEmail}
                            }
                        }
                    }, {
                        "TemplateID",
                        6953989
                    }, {
                        "TemplateLanguage",
                        true
                    }, {
                        "Subject",
                        subject
                    }, {
                        "Variables",
                        new JObject {
                            {"confirmation_link", link}, 
                            {"current_year", DateTime.UtcNow.Year}
                        }
                    }
                }
            });

            MailjetResponse response = await client.PostAsync(request);
        }
    }
}