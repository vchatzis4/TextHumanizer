using System.Text.Json;
using TextHumanizer.Interfaces;
using TextHumanizer.Models;
using TextHumanizer.Models.Requests;
using TextHumanizer.Models.Responses;

namespace TextHumanizer.Services;

public class LlmService : ILlmService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<LlmService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly string? _model;

    public LlmService(HttpClient httpClient, ILogger<LlmService> logger, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _logger = logger;
        _model = configuration.GetValue<string>("LlmProvider:Model");
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task<HumanizeResponse> HumanizeTextAsync(HumanizeRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Humanizing text with tone: {Tone}", request.Tone);

        var prompt = PromptTemplates.GetHumanizePrompt(request.Text, request.Tone.ToString().ToLower());
        var humanizedText = await SendChatCompletionAsync(prompt, cancellationToken);

        return new HumanizeResponse
        {
            OriginalText = request.Text,
            HumanizedText = humanizedText
        };
    }

    public async Task<DetectResponse> DetectAiTextAsync(DetectRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Detecting AI text patterns");

        var prompt = PromptTemplates.GetDetectPrompt(request.Text);
        var response = await SendChatCompletionAsync(prompt, cancellationToken);

        try
        {
            var cleanedResponse = CleanJsonResponse(response);
            var detectResult = JsonSerializer.Deserialize<DetectJsonResult>(cleanedResponse, _jsonOptions);

            if (detectResult == null)
            {
                throw new InvalidOperationException("Failed to parse detection response");
            }

            return new DetectResponse
            {
                AiProbability = Math.Clamp(detectResult.Score, 0, 100),
                Reasons = detectResult.Reasons ?? new List<string>()
            };
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse LLM detection response: {Response}", response);
            throw new InvalidOperationException("Failed to parse AI detection response from LLM", ex);
        }
    }

    private async Task<string> SendChatCompletionAsync(string prompt, CancellationToken cancellationToken)
    {
        var chatRequest = new ChatCompletionRequest
        {
            Model = _model,
            Messages = new List<ChatMessage>
            {
                new ChatMessage { Role = "user", Content = prompt }
            }
        };

        var jsonContent = JsonSerializer.Serialize(chatRequest, _jsonOptions);
        using var content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");

        _logger.LogDebug("Sending request to LLM provider");

        var response = await _httpClient.PostAsync("v1/chat/completions", content, cancellationToken);
        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        var chatResponse = JsonSerializer.Deserialize<ChatCompletionResponse>(responseBody, _jsonOptions);

        var messageContent = chatResponse?.Choices?.FirstOrDefault()?.Message?.Content;

        if (string.IsNullOrEmpty(messageContent))
        {
            throw new InvalidOperationException("Empty response received from LLM");
        }

        return messageContent;
    }

    private static string CleanJsonResponse(string response)
    {
        var cleaned = response.Trim();

        // Remove markdown code blocks
        if (cleaned.StartsWith("```json"))
        {
            cleaned = cleaned.Substring(7);
        }
        else if (cleaned.StartsWith("```"))
        {
            cleaned = cleaned.Substring(3);
        }

        if (cleaned.EndsWith("```"))
        {
            cleaned = cleaned.Substring(0, cleaned.Length - 3);
        }

        cleaned = cleaned.Trim();

        // Extract JSON object if there's text before/after it
        var startIndex = cleaned.IndexOf('{');
        var endIndex = cleaned.LastIndexOf('}');

        if (startIndex >= 0 && endIndex > startIndex)
        {
            cleaned = cleaned.Substring(startIndex, endIndex - startIndex + 1);
        }

        return cleaned;
    }
}
