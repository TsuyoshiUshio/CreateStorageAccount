using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.Storage.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Azure.Management.Storage;
using Microsoft.Azure.Management.ResourceManager;
using Microsoft.Azure.Management.ResourceManager.Models;

using System.Linq;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;

namespace CreateStorageAccount
{
    class Program
    {
        static void Main(string[] args)
        {
            new Program().MainAsync().GetAwaiter().GetResult();
        }

        public static IConfigurationRoot Configuration { get; set; }

        static Program()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json");
            Configuration = builder.Build();
             resourceGroup = Configuration["resourceGroup"];
             subscriptionId = Configuration["subscriptionId"];
             clientId = Configuration["clientId"];
             clientSecret = Configuration["clientSecret"];
             tenantId = Configuration["tenantId"];
        }

        private async Task CreateOrUpdateResourceGroupAsync(AzureCredentials credentials, string location)
        {
            using (var resourceClient = new Microsoft.Azure.Management.ResourceManager.ResourceManagementClient(credentials))
            {
                var parameter = new ResourceGroup();
                parameter.Location = location;
                parameter.Name = resourceGroup;
                resourceClient.SubscriptionId = subscriptionId;
                await resourceClient.ResourceGroups.CreateOrUpdateAsync(resourceGroup, parameter);
            }
        }

        private async Task MainAsync()
        {
            var credentials = SdkContext.AzureCredentialsFactory.FromServicePrincipal(clientId, clientSecret, tenantId, AzureEnvironment.AzureGlobalCloud);

            var location = "NorthEurope";

            await CreateOrUpdateResourceGroupAsync(credentials, location);

            using (var client = new StorageManagementClient(credentials))
            {

                var accountName = "someaacountabc2";
                
                var storageAccount = await CreateStorageAccountAsync(client, accountName, location);

                var key = await GetAccountKeyAsync(client, accountName);
                printStorageAccountInfo(accountName, key);
            }
            
        }
        private void printStorageAccountInfo(string accountName, StorageAccountKey key)
        {
            Console.WriteLine($"Storage Account was created. Name: {accountName} ConnectionString: {key.Value}");
            Console.WriteLine("Press Any Key");
            Console.ReadLine();
        }
        private async Task<StorageAccount> CreateStorageAccountAsync(StorageManagementClient client, string accountName, string location)
        {
            var parameter = new StorageAccountCreateParameters();
            parameter.Location = location;
            parameter.Kind = Kind.Storage; // WE can also specify StorageV2
            parameter.Sku = new Microsoft.Azure.Management.Storage.Models.Sku(SkuName.StandardLRS, SkuTier.Standard);
            client.SubscriptionId = subscriptionId;
            return await client.StorageAccounts.CreateAsync(resourceGroup, accountName, parameter);
        }

        private async Task<StorageAccountKey> GetAccountKeyAsync(StorageManagementClient client, string accountName)
        {
            var properties = await client.StorageAccounts.GetPropertiesAsync(resourceGroup, accountName);
            var keys = await client.StorageAccounts.ListKeysAsync(resourceGroup, accountName);
            var storageAccountKeys = keys.Keys;
            return storageAccountKeys.First();
        }

        private static string resourceGroup;
        private static string subscriptionId;
        private static string clientId;
        private static string clientSecret;
        private static string tenantId;
    }
}
