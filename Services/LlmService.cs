using System.Text.Json;
using System.Text.RegularExpressions;
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
        var isGreekInput = IsGreekText(request.Text);

        const int maxRetries = 3;
        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            var humanizedText = await SendChatCompletionAsync(prompt, cancellationToken);

            // Validate output language matches input
            if (isGreekInput)
            {
                var validationResult = ValidateGreekText(humanizedText);
                if (!validationResult.IsValid)
                {
                    _logger.LogWarning("Attempt {Attempt}: Greek text validation failed - {Reason}", attempt, validationResult.Reason);

                    if (attempt < maxRetries)
                        continue;

                    // On final attempt, clean the text instead of failing
                    humanizedText = CleanGreekText(humanizedText);
                }
            }

            return new HumanizeResponse
            {
                OriginalText = request.Text,
                HumanizedText = humanizedText
            };
        }

        throw new InvalidOperationException("Failed to generate valid humanized text after multiple attempts");
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

    private static bool IsGreekText(string text)
    {
        // Count Greek vs Latin characters
        int greekCount = 0;
        int latinCount = 0;

        foreach (char c in text)
        {
            if (c >= 'Α' && c <= 'ω' || c >= 'ά' && c <= 'ώ') // Greek Unicode range
                greekCount++;
            else if (c >= 'A' && c <= 'z')
                latinCount++;
        }

        return greekCount > latinCount;
    }

    private static (bool IsValid, string Reason) ValidateGreekText(string text)
    {
        // Check for Latin letters (a-z, A-Z) - these shouldn't appear in Greek text
        var latinMatch = Regex.Match(text, @"[a-zA-Z]{2,}"); // 2+ consecutive Latin chars = likely English word
        if (latinMatch.Success)
        {
            return (false, $"Contains English word: '{latinMatch.Value}'");
        }

        // Check for Latin letters with diacritics (Vietnamese, French, etc.)
        // Latin Extended: ÀàÁáÂâÃãÄäÅåÆæÇç... and Vietnamese ơưăđ...
        var latinDiacriticMatch = Regex.Match(text, @"[\u00C0-\u024F\u1E00-\u1EFF]{1,}");
        if (latinDiacriticMatch.Success)
        {
            return (false, $"Contains Latin diacritic character: '{latinDiacriticMatch.Value}'");
        }

        // Check for Cyrillic characters
        var cyrillicMatch = Regex.Match(text, @"[\u0400-\u04FF]");
        if (cyrillicMatch.Success)
        {
            return (false, $"Contains Cyrillic character: '{cyrillicMatch.Value}'");
        }

        // Check for Devanagari (Hindi) characters
        var devanagariMatch = Regex.Match(text, @"[\u0900-\u097F]");
        if (devanagariMatch.Success)
        {
            return (false, $"Contains Devanagari character: '{devanagariMatch.Value}'");
        }

        // Check for other non-Greek scripts (Arabic, Chinese, Japanese, Korean, Thai, Hebrew)
        var otherScriptMatch = Regex.Match(text, @"[\u0600-\u06FF\u4E00-\u9FFF\u3040-\u309F\u30A0-\u30FF\uAC00-\uD7AF\u0E00-\u0E7F\u0590-\u05FF]");
        if (otherScriptMatch.Success)
        {
            return (false, $"Contains non-Greek script character: '{otherScriptMatch.Value}'");
        }

        return (true, string.Empty);
    }

    private static string CleanGreekText(string text)
    {
        // Remove non-Greek alphabetic characters while keeping punctuation, numbers, and spaces
        // Covers: Basic Latin, Latin Extended, Vietnamese, Cyrillic, Devanagari, Arabic, CJK, Korean, Thai, Hebrew
        var cleaned = Regex.Replace(text,
            @"[a-zA-Z\u00C0-\u024F\u1E00-\u1EFF\u0400-\u04FF\u0900-\u097F\u0600-\u06FF\u4E00-\u9FFF\u3040-\u30FF\uAC00-\uD7AF\u0E00-\u0E7F\u0590-\u05FF]+",
            "");

        // Clean up any double spaces created
        cleaned = Regex.Replace(cleaned, @"\s{2,}", " ");

        return cleaned.Trim();
    }
}
