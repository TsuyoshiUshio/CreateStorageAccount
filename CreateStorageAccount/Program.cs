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
using System.Collections.Concurrent;

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

                var accountNameHeader = "efitabdesae";

                // First, I try to create 100 storage account, however, the limit of storage account is 200 per subscription. 
                // Also I've got Cloud Exception: The request is being throttled. 
                // https://docs.microsoft.com/en-us/azure/azure-resource-manager/resource-manager-request-limits
                // https://docs.microsoft.com/en-us/azure/azure-subscription-service-limits
                ConcurrentBag<Task> taskList = new ConcurrentBag<Task>();
                Parallel.For(0, 20, i =>
                {
                    taskList.Add(Task.Run(async () =>
                    {
                        var accountName = $"{accountNameHeader}{i}";
                        var storageAccount = await CreateStorageAccountAsync(client, accountName, location);
                        var key = await GetAccountKeyAsync(client, accountName);
                        printStorageAccountInfo(accountName, key);
                    }));
                });
                await Task.WhenAll(taskList.ToArray());
                Console.WriteLine("All Storage Accounts created. Press Any Key");
                Console.ReadLine();
            }
            
        }
        private void printStorageAccountInfo(string accountName, StorageAccountKey key)
        {
            Console.WriteLine($"Storage Account was created. Name: {accountName} ConnectionString: {key.Value}");
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
