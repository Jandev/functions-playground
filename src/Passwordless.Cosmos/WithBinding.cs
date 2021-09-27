using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Passwordless.Cosmos
{
    public static class WithBinding
    {
        [FunctionName(nameof(WithBinding))]
        public static async IAsyncEnumerable<User> Run(
            [HttpTrigger(AuthorizationLevel.Function, "GET")] 
            HttpRequest req,
            [CosmosDB(
                databaseName: "Music",
                containerName: "Users",
                Connection = "FunctionsPlaygroundRepository"
                )]
            CosmosClient client,
            ILogger log)
        {
            string name = req.Query["name"];

            // Inspired by the docs: https://docs.microsoft.com/en-us/azure/azure-functions/functions-bindings-cosmosdb-v2-input?tabs=csharp&WT.mc_id=AZ-MVP-5003246#http-trigger-get-multiple-docs-using-cosmosclient
            var container = client.GetDatabase("Music").GetContainer("Users");
            log.LogInformation($"Searching for: {name}");

            QueryDefinition queryDefinition = new QueryDefinition(
                    "SELECT * FROM items i WHERE CONTAINS(i.Firstname, @name)")
                .WithParameter("@name", name);

            using var resultSet = container.GetItemQueryIterator<User>(queryDefinition);
            while (resultSet.HasMoreResults)
            {
                var response = await resultSet.ReadNextAsync();
                foreach (var user in response.Resource)
                {
                    yield return user;
                }
            }
        }
    }

    public class User
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("partitionKey")]
        public string PartitionKey { get; set; }
        public string Firstname { get; set; }
        public string Lastname { get; set; }
    }
}
