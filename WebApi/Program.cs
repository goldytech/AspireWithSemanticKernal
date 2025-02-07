using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
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
        var serversList = new List<ScalarServer>
        {
            
            new(app.Urls.FirstOrDefault()!)
          
        };
        
         
        options.Servers = serversList;
        
    });
    
    app.UseSwaggerUI(o =>
    {
        o.SwaggerEndpoint("/openapi/v1.json", "OpenAPI v1");
        
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
        new SystemChatMessage("You are helpful assistant"),
        new UserChatMessage(prompt.Prompt)
    };
    var response = await chatClient.CompleteChatAsync(messages);
    messages.Add(new AssistantChatMessage(response.Value.Content[0].Text));
    return response.Value.Content[0].Text;
}).WithName("Chat");


app.MapPost("/chat2", async (HttpContext context,[FromServices]Kernel kernel, UserPrompt prompt) =>
{
    ChatHistory chatHistory = [];
    chatHistory.AddSystemMessage("You are helpful assistant");
    chatHistory.AddUserMessage(prompt.Prompt);
   
    var chatCompletionService = kernel.Services.GetRequiredService<IChatCompletionService>();
    var response = await chatCompletionService.GetChatMessageContentAsync(chatHistory,kernel:kernel);
    chatHistory.AddAssistantMessage(response.Content!);
    return response.Content!;
    
    

    

}).WithName("Chat2");

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
record UserPrompt(string Prompt);