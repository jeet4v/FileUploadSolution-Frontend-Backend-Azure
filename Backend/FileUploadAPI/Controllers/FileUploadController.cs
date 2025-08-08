using Azure.Messaging.ServiceBus;
using Azure.Storage.Blobs;
using FileUploadAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Text;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;

namespace FileUploadAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FileUploadController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public FileUploadController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpPost]
        [Route("upload")]
        public async Task<IActionResult> upload(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest("No file uploaded.");
                }

                #region Getting Azure Blob Connection String from Azure Key Vault.
                var clientId = _configuration["AzureAD:ClientId"];
                var tenantId = _configuration["AzureAD:TenantId"];
                var clientSecret = _configuration["AzureAD:ClientSecret"];

                var kvUri = "https://ky-fileupload.vault.azure.net/";
                var client = new SecretClient(new Uri(kvUri), new ClientSecretCredential(tenantId, clientId, clientSecret));
                KeyVaultSecret kvs = client.GetSecret("ConnectionString-AzureBlob");
                var connectionString = kvs.Value;
                #endregion

                //var connectionString = _configuration["AzureBlob:ConnectionString"];
                var containerName = _configuration["AzureBlob:ContainerName"];

                var blobClient = new BlobContainerClient(connectionString, containerName);
                await blobClient.CreateIfNotExistsAsync();
                var blob = blobClient.GetBlobClient(file.FileName);

                using (var stream = file.OpenReadStream())
                {
                    await blob.UploadAsync(stream, overwrite: true);
                }

                #region send email notification from Azure Function with service bus queue.
                var serviceBusConnectionString = _configuration["ServiceBus:ConnectionString"];
                var queueName = _configuration["ServiceBus:QueueName"];
                var serviceBusClient = new ServiceBusClient(serviceBusConnectionString);
                var serviceBusSender = serviceBusClient.CreateSender(queueName);

                EmailProp emailProp = new EmailProp();
                emailProp.To = "";
                emailProp.Subject = "Azure Blob Storage || File Uploaded";
                emailProp.Body = $"File \"{file.FileName}\" uploaded on Azure Blob Storage successfully !!!";
                var emailPropJSON = JsonConvert.SerializeObject(emailProp);
                var serviceBusMessage = new ServiceBusMessage(Encoding.UTF8.GetBytes(emailPropJSON));
                await serviceBusSender.SendMessageAsync(serviceBusMessage);
                #endregion

                return Ok(new { message = "File uploaded successfully to Azure Blob!" });
            }
            catch (Exception ex)
            {
                return BadRequest("No file uploaded.");
            }
            
        }
    }
}
