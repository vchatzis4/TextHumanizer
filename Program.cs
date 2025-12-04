using TextHumanizer.Interfaces;
using TextHumanizer.Services;

var builder = WebApplication.CreateBuilder(args);

// Add CORS for Blazor UI
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Add services to the container
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure LLM Provider HttpClient
var provider = builder.Configuration.GetValue<string>("LlmProvider:Provider") ?? "LmStudio";

builder.Services.AddHttpClient<ILlmService, LlmService>(client =>
{
    if (provider.Equals("Groq", StringComparison.OrdinalIgnoreCase))
    {
        var baseUrl = builder.Configuration.GetValue<string>("LlmProvider:Groq:BaseUrl")
            ?? "https://api.groq.com/openai/";
        var apiKey = builder.Configuration.GetValue<string>("LlmProvider:Groq:ApiKey");

        client.BaseAddress = new Uri(baseUrl);
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
    }
    else // LmStudio (default)
    {
        var baseUrl = builder.Configuration.GetValue<string>("LlmProvider:LmStudio:BaseUrl")
            ?? "http://localhost:1234/";
        client.BaseAddress = new Uri(baseUrl);
    }

    client.Timeout = TimeSpan.FromMinutes(2);
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Global exception handler
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/json";

        var error = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
        var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();

        if (error != null)
        {
            logger.LogError(error.Error, "Unhandled exception occurred");

            var problemDetails = new Microsoft.AspNetCore.Mvc.ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "An error occurred while processing your request",
                Detail = app.Environment.IsDevelopment() ? error.Error.Message : null
            };

            await context.Response.WriteAsJsonAsync(problemDetails);
        }
    });
});

app.UseCors();
app.UseAuthorization();
app.MapControllers();

app.Run();
