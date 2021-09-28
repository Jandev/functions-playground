using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Passwordless.Cosmos
{
    public class Users
    {
        private readonly ILogger<Users> logger;

        public Users(ILogger<Users> logger)
        {
            this.logger = logger;
        }

        /// <remarks>
        /// I couldn't get the `HTTP trigger, look up ID from query string` from the docs (https://docs.microsoft.com/en-us/azure/azure-functions/functions-bindings-cosmosdb-v2-input?tabs=csharp&WT.mc_id=AZ-MVP-5003246#http-trigger-look-up-id-from-query-string)
        /// to work with the current libraries, so using the <see cref="CosmosClient"/> version over here also.
        /// </remarks>
        [FunctionName(nameof(GetUser))]
        public async Task<IActionResult> GetUser(
            [HttpTrigger(AuthorizationLevel.Function, "GET")]
            HttpRequest req,
            [CosmosDB(Connection = "FunctionsPlaygroundRepository")]
            CosmosClient client
            )
        {
            string id = req.Query["id"];
            string partitionKey = req.Query["partitionKey"];

            var container = client.GetDatabase("Music").GetContainer("Users");

            QueryDefinition queryDefinition = new QueryDefinition(
                    "SELECT * FROM items i WHERE i.id = @id AND i.partitionKey = @partitionKey")
                .WithParameter("@id", id)
                .WithParameter("@partitionKey", partitionKey);

            using var resultSet = container.GetItemQueryIterator<User>(queryDefinition);
            var response = await resultSet.ReadNextAsync();
            var user = response.Resource.Single();

            return new OkObjectResult(user);
        }

        [FunctionName(nameof(GetUsers))]
        public async IAsyncEnumerable<User> GetUsers(
            [HttpTrigger(AuthorizationLevel.Function, "GET")] 
            HttpRequest req,
            [CosmosDB(Connection = "FunctionsPlaygroundRepository")]
            CosmosClient client)
        {
            string name = req.Query["name"];

            // Inspired by the docs: https://docs.microsoft.com/en-us/azure/azure-functions/functions-bindings-cosmosdb-v2-input?tabs=csharp&WT.mc_id=AZ-MVP-5003246#http-trigger-get-multiple-docs-using-cosmosclient
            var container = client.GetDatabase("Music").GetContainer("Users");
            logger.LogInformation($"Searching for: {name}");

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

        [FunctionName(nameof(PostUser))]
        public IActionResult PostUser(
            [HttpTrigger(AuthorizationLevel.Function, "POST")]
            HttpRequest req,
            [CosmosDB(
                databaseName: "Music",
                containerName: "Users",
                Connection = "FunctionsPlaygroundRepository")]
            out dynamic newUser)
        {
            // Inspired by the docs: https://docs.microsoft.com/en-us/azure/azure-functions/functions-bindings-cosmosdb-v2-output?tabs=csharp&WT.mc_id=AZ-MVP-5003246#queue-trigger-write-one-doc-v4-extension
            var payload = req.ReadAsStringAsync().Result;
            var userPayload = JsonConvert.DeserializeObject<User>(payload);

            userPayload.Id = Guid.NewGuid().ToString("D");
            userPayload.PartitionKey = Guid.NewGuid().ToString("D");
            logger.LogInformation("Adding {Firstname} to the repository with {Id} in {Partition}.", userPayload.Firstname, userPayload.Id, userPayload.PartitionKey);

            newUser = new { userPayload.Firstname, userPayload.Lastname, partitionKey = userPayload.PartitionKey, id = userPayload.Id };
            return new CreatedResult($"api/{nameof(GetUser)}?id={userPayload.Id}&partitionKey={userPayload.PartitionKey}", userPayload);
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
