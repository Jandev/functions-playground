using Azure.Core;
using Azure.Identity;
using KeyVaultUsage;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using System;

[assembly: FunctionsStartup(typeof(Startup))]
namespace KeyVaultUsage
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
        }

        public override void ConfigureAppConfiguration(IFunctionsConfigurationBuilder builder)
        {
            // Don't do this!
            //builder.ConfigurationBuilder
            //    .AddAzureAppConfiguration("Endpoint=https://functions-configuration.azconfig.io;Id=aymV-l9-s0:Uz/FhcX+lasz6dyikb/P;Secret=819p+Bc8VYf40Ap2/tCQ1L9JPRSzKa1Vx8obVIJPzBs=");
            // With Key Vault references, you'll get an error:
            /*
             Microsoft.Extensions.Configuration.AzureAppConfiguration: 
            No key vault credential or secret resolver callback configured, and no matching secret client could be found.. 
            ErrorCode:, Key:referenced-secret, Label:, Etag:TZPoUQqbv2gxYmJPYwVV9zGiCjO, 
            SecretIdentifier:https://functions-playground-kv.vault.azure.net/secrets/referenced-secret. 
            Microsoft.Extensions.Configuration.AzureAppConfiguration: No key vault credential or secret resolver callback 
            configured, and no matching secret client could be found
             */

            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            // To solve this, do the following
            // https://jan-v.nl/post/2021/using-key-vault-with-azure-app-configuration/
            builder.ConfigurationBuilder
                .AddAzureAppConfiguration(
                options =>
                {
                    ConfigureOptions(options);
                });

        }

        private static void ConfigureOptions(AzureAppConfigurationOptions options)
        {
            var credentials = GetAzureCredentials();

            // I couldn't get this one to work on my demo environment, but it SHOULD work!
            // options.Connect(appConfigEndpoint, credentials);
            options.Connect("Endpoint=https://functions-configuration.azconfig.io;Id=aymV-l9-s0:Uz/FhcX+lasz6dyikb/P;Secret=819p+Bc8VYf40Ap2/tCQ1L9JPRSzKa1Vx8obVIJPzBs=");
            options.ConfigureKeyVault(kv => kv.SetCredential(credentials));
        }

        private static TokenCredential GetAzureCredentials()
        {
            var isDeployed = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WEBSITE_SITE_NAME"));
            return new DefaultAzureCredential(
                new DefaultAzureCredentialOptions
                {
                    // Prevent deployed instances from trying things that don't work and generally take too long
                    ExcludeInteractiveBrowserCredential = isDeployed,
                    ExcludeVisualStudioCodeCredential = isDeployed,
                    ExcludeVisualStudioCredential = isDeployed,
                    ExcludeSharedTokenCacheCredential = isDeployed,
                    ExcludeAzureCliCredential = isDeployed,
                    ExcludeManagedIdentityCredential = false,
                    Retry =
                    {
				        // Reduce retries and timeouts to get faster failures
				        MaxRetries = 2,
                        NetworkTimeout = TimeSpan.FromSeconds(5),
                        MaxDelay = TimeSpan.FromSeconds(5)
                    },

                    // this helps devs use the right tenant
                    InteractiveBrowserTenantId = DefaultTenantId,
                    SharedTokenCacheTenantId = DefaultTenantId,
                    VisualStudioCodeTenantId = DefaultTenantId,
                    VisualStudioTenantId = DefaultTenantId
                }
            );
        }

        private const string DefaultTenantId = "4b1fa0f3-862b-4951-a3a8-df1c72935c79";
    }
}
