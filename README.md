# Azure Functions Playground

I'm testing out some features in this repository of stuff I want to try, or want to write about and need demo projects.

# Used resources

## Azure Functions

The passwordless features are still new and requires an [Elastic Premium instance of Azure Functions at this time](https://docs.microsoft.com/en-us/azure/azure-functions/functions-reference?WT.mc_id=AZ-MVP-5007226#grant-permission-to-the-identity). You also need the [v4 library of the Cosmos DB extensions package](https://www.nuget.org/packages/Microsoft.Azure.WebJobs.Extensions.CosmosDB).

The Functions App needs the Managed Identity set to `true` to use this passwordless feature. It's also possible to use a regular service principal, but I won't be using that over here.

Be sure to the the correct configuration.

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet",
    "FunctionsPlaygroundRepository__accountEndpoint": "https://my-cosmos-repository.documents.azure.com:443/"
  }
}
```

## Cosmos DB

In this project I'm using a Cosmos DB instance with a database with the SQL API called `Music`.  
This database contains the following collections:

- Collections
- OwnedMedia
- Users

### Assigning roles

At this moment in time there isn't any support to assign roles for the data plane in the Azure Portal. This has to be done via a script (PowerShell or Azure CLI), or via ARM template.

The necessary script for the [Azure CLI looks like the following](https://docs.microsoft.com/en-us/azure/cosmos-db/how-to-setup-rbac?WT.mc_id=AZ-MVP-5007226#using-the-azure-cli-1).

```powershell
$resourceGroupName='<myResourceGroup>'
$accountName='<myCosmosAccount>'
# Found over here: https://docs.microsoft.com/en-us/azure/cosmos-db/how-to-setup-rbac?WT.mc_id=AZ-MVP-5007226#built-in-role-definitions
# Cosmos DB Built-in Data Reader: 00000000-0000-0000-0000-000000000001
# Cosmos DB Built-in Data Contributor: 00000000-0000-0000-0000-000000000002
$readOnlyRoleDefinitionId = '<roleDefinitionId>'
$principalId = '<aadPrincipalId>'
az cosmosdb sql role assignment create --account-name $accountName --resource-group $resourceGroupName --scope "/" --principal-id $principalId --role-definition-id $readOnlyRoleDefinitionId
```

Be sure to have an updated Azure CLI version to get support for this command. If you don't have this, type `az upgrade` and the installer will do the work for you.

Once the command has completed and output will be shown like the following.

```json
{
  "id": "/subscriptions/7b7729b2-021a-28b5-a2eb-27be0c7e7f22/resourceGroups/functions-playground/providers/Microsoft.DocumentDB/databaseAccounts/my-cosmos-repository/sqlRoleAssignments/5b97552a-2f7b-28a8-8989-b20c16bce26c",
  "name": "5b97552a-2f7b-28a8-8989-b20c16bce26c",
  "principalId": "ebfcbe6f-e2b8-2679-a81c-97221d8d8726",
  "resourceGroup": "functions-playground",
  "roleDefinitionId": "/subscriptions/7b7729b2-021a-28b5-a2eb-27be0c7e7f22/resourceGroups/functions-playground/providers/Microsoft.DocumentDB/databaseAccounts/my-cosmos-repository/sqlRoleDefinitions/00000000-0000-0000-0000-000000000002",
  "scope": "/subscriptions/7b7729b2-021a-28b5-a2eb-27be0c7e7f22/resourceGroups/functions-playground/providers/Microsoft.DocumentDB/databaseAccounts/my-cosmos-repository",
  "type": "Microsoft.DocumentDB/databaseAccounts/sqlRoleAssignments"
}
```

The role will not show up in the portal due to the lack of support.

If you want to test the access on your local machine, make sure to grant your own account access to the Cosmos DB instance also.
