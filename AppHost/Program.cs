var builder = DistributedApplication.CreateBuilder(args);
// dotnet user-secrets set  "ConnectionStrings:azureOpenAi" "Endpoint=https://{openai endpoint}.openai.azure.com/;Key={ApiKey}
var azureOpenAiConnStr = builder.Configuration["ConnectionStrings:azureOpenAi"];

var openai =builder.AddConnectionString("azureOpenAi");
var api = builder.AddProject<Projects.WebApi>("api")
    .WithReference(openai)
    .WithEnvironment("AZ_OPENAI_CONNSTR", azureOpenAiConnStr)
    .WithExternalHttpEndpoints();

builder.Build().Run();