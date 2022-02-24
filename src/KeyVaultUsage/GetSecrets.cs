using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Azure.KeyVault;
using Microsoft.Extensions.Configuration;

namespace KeyVaultUsage
{
    public class GetSecrets
    {
        private readonly IConfiguration configuration;

        public GetSecrets(IConfiguration configuration)
        {
            this.configuration=configuration;
        }

        [FunctionName(nameof(GetSecrets))]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] 
            HttpRequest req,
            ILogger log)
        {
            string allOfMySecrets = default(string);            
            var azureServiceTokenProvider = new AzureServiceTokenProvider();
            // This is what we'll be using later on, getting an access token via a resource.
            string accessToken = await azureServiceTokenProvider.GetAccessTokenAsync("https://vault.azure.net");
            // OR
            var kv = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(azureServiceTokenProvider.KeyVaultTokenCallback));
            var mySecret = await kv.GetSecretAsync("https://functions-playground-kv.vault.azure.net/", "secret");

            allOfMySecrets += $"The secret from Key Vault = {mySecret.Value}{Environment.NewLine}";

            // Retrieving a key vault reference via App Configuration
            var referencedSecretValue = configuration.GetValue<string>("referenced-secret");
            allOfMySecrets += $"The secret from App Configuration -> Key Vault Reference = {referencedSecretValue}{Environment.NewLine}";

            return new OkObjectResult(allOfMySecrets);
        }
    }
}
