using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.SemanticKernel;
using OpenAI.Chat;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);


builder.AddServiceDefaults();

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi("v1");

// Extract AzureOpenAI Connection String from Environment Variable
var azureOpenAiConnStr = builder.Configuration["AZ_OPENAI_CONNSTR"];
// Extract the Endpoint and Key from the Connection String Eg Endpoint=https://{openai endpoint}.openai.azure.com/;Key={ApiKey}
if (azureOpenAiConnStr != null)
{
    var endpoint = azureOpenAiConnStr.Split(";")[0];
    var apiKey = azureOpenAiConnStr.Split(";")[1].Split(";").First();
    builder.AddAzureOpenAIClient("azureOpenAi", configureSettings: settings =>
    {
        settings.Credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions { ExcludeVisualStudioCredential = true });
    });

    builder.Services.AddKernel()
    
        .AddAzureOpenAIChatCompletion("gpt-4o", endpoint,apiKey)
        .ConfigureOpenTelemetry(builder.Configuration);

}
else
{
    // Throw the exception
    throw new Exception("Azure OpenAI Connection String is not found in the Environment Variable AZ_OPENAI_CONNSTR");
  
}


var app = builder.Build();


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.Title = "Weather Forecast API";
        
    });
    
}

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
    {
        var forecast = Enumerable.Range(1, 5).Select(index =>
                new WeatherForecast
                (
                    DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                    Random.Shared.Next(-20, 55),
                    summaries[Random.Shared.Next(summaries.Length)]
                ))
            .ToArray();
        return forecast;
    })
    .WithName("GetWeatherForecast");

app.MapPost("/chat", async (HttpContext context, [FromServices]AzureOpenAIClient client,UserPrompt prompt) =>
{
    var chatClient = client.GetChatClient("gpt-4o");
    var messages = new List<ChatMessage>
    {
        new UserChatMessage(prompt.Prompt)
    };
    var response = await chatClient.CompleteChatAsync(messages);
    return response.Value.Content[0].Text;
}).WithName("Chat");


app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
record UserPrompt(string Prompt);