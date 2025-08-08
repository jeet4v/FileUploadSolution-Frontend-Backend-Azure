using AF_ServiceBus_QueueTrigger_EmailNotification.Models;
using Azure.Communication.Email;
using Azure;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace AF_ServiceBus_QueueTrigger_EmailNotification
{
    public class EmailNotificationHttpTrigger
    {
        //private readonly ILogger<EmailNotificationHttpTrigger> _logger;

        //public EmailNotificationHttpTrigger(ILogger<EmailNotificationHttpTrigger> logger)
        //{
        //    _logger = logger;
        //}

        [Function(nameof(EmailNotificationHttpTrigger))]
        public async Task Run(
            [ServiceBusTrigger("queuefileuploadnotification", Connection = "ServiceBusConnectionString")]
            EmailProp email,
            ServiceBusMessageActions messageActions,
            ILogger log)
        {
            log.LogInformation("EmailNotificationHttpTrigger triggered.");

            var connectionString = Environment.GetEnvironmentVariable("ACS_CONNECTION_STRING");
            var senderEmail = Environment.GetEnvironmentVariable("EMAIL_SENDER");

            var emailClient = new EmailClient(connectionString);

            var emailContent = new EmailContent(email.Subject) {
                PlainText = email.Body
            };

            var emailRecipients = new EmailRecipients(new[] {
                new EmailAddress(email.To) 
            });

            var emailMessage = new EmailMessage(senderEmail, emailRecipients, emailContent);

            try
            {
                var response = await emailClient.SendAsync(WaitUntil.Completed, emailMessage);
                log.LogInformation($"Email sent to {email.To}");
            }
            catch (Exception ex)
            {
                log.LogError($"Exception while sending email notification: {ex.Message} \n {ex.StackTrace} \n {ex.InnerException}");
            }


        }
    }
}
