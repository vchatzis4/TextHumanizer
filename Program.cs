using TextHumanizer.Interfaces;
using TextHumanizer.Services;

var builder = WebApplication.CreateBuilder(args);

// Add CORS for Blazor UI
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();

        if (allowedOrigins != null && allowedOrigins.Length > 0)
        {
            // Production: restrict to specific origins
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        }
        else
        {
            // Development: allow any origin
            policy.AllowAnyOrigin()
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        }
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

// Register AI detection services
builder.Services.AddSingleton<ITextAnalysisService, TextAnalysisService>();
builder.Services.AddSingleton<IAiDetectionService, AiDetectionService>();

// Configure LLM Provider HttpClient
var provider = builder.Configuration.GetValue<string>("LlmProvider:Provider") ?? "LmStudio";

builder.Services.AddHttpClient<ILlmService, LlmService>(client =>
{
    if (provider.Equals("Groq", StringComparison.OrdinalIgnoreCase))
    {
        var baseUrl = builder.Configuration.GetValue<string>("LlmProvider:Groq:BaseUrl")
            ?? "https://api.groq.com/openai/";

        // Check environment variable first, then fall back to config
        var apiKey = Environment.GetEnvironmentVariable("GROQ_API_KEY")
            ?? builder.Configuration.GetValue<string>("LlmProvider:Groq:ApiKey");

        if (string.IsNullOrEmpty(apiKey))
        {
            throw new InvalidOperationException("GROQ_API_KEY environment variable or LlmProvider:Groq:ApiKey config is required");
        }

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
else
{
    // Production: Handle HTTPS behind reverse proxy (Azure, Railway, etc.)
    app.UseForwardedHeaders(new ForwardedHeadersOptions
    {
        ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor
            | Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto
    });

    // Redirect HTTP to HTTPS
    app.UseHttpsRedirection();
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
