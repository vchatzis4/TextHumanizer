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
    private readonly ITextAnalysisService _textAnalysisService;
    private readonly IAiDetectionService _aiDetectionService;

    public LlmService(
        HttpClient httpClient,
        ILogger<LlmService> logger,
        IConfiguration configuration,
        ITextAnalysisService textAnalysisService,
        IAiDetectionService aiDetectionService)
    {
        _httpClient = httpClient;
        _logger = logger;
        _model = configuration.GetValue<string>("LlmProvider:Model");
        _textAnalysisService = textAnalysisService;
        _aiDetectionService = aiDetectionService;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task<HumanizeResponse> HumanizeTextAsync(HumanizeRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Humanizing text with tone: {Tone}", request.Tone);

        // Pre-analysis: identify AI patterns in the input
        var analysis = _textAnalysisService.Analyze(request.Text);
        var detectionResult = _aiDetectionService.CalculateAiProbability(analysis);
        var analysisHints = GenerateHumanizationHints(detectionResult, analysis);

        _logger.LogDebug("Pre-analysis complete. AI Score: {Score}, Hints: {Hints}",
            detectionResult.AiProbability, analysisHints ?? "none");

        var prompt = PromptTemplates.GetHumanizePrompt(request.Text, request.Tone.ToString().ToLower(), analysisHints);
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
        _logger.LogInformation("Detecting AI text patterns using statistical analysis + LLM");

        // Step 1: Perform statistical text analysis
        var textAnalysis = _textAnalysisService.Analyze(request.Text);
        _logger.LogDebug("Statistical analysis complete: {WordCount} words, {Sentences} sentences",
            textAnalysis.TotalWords, textAnalysis.TotalSentences);

        // Step 2: Get LLM analysis for pattern detection
        var prompt = PromptTemplates.GetDetectPrompt(request.Text);
        var isGreekInput = IsGreekText(request.Text);
        int? llmProbability = null;
        var reasons = new List<string>();

        const int maxRetries = 3;
        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                var response = await SendChatCompletionAsync(prompt, cancellationToken);
                var cleanedResponse = CleanJsonResponse(response);
                var detectResult = JsonSerializer.Deserialize<DetectJsonResult>(cleanedResponse, _jsonOptions);

                if (detectResult != null)
                {
                    llmProbability = Math.Clamp(detectResult.Score, 0, 100);
                    reasons = detectResult.Reasons ?? new List<string>();

                    // Validate reasons language matches input
                    if (isGreekInput && reasons.Count > 0)
                    {
                        var reasonsText = string.Join(" ", reasons);
                        var validationResult = ValidateGreekText(reasonsText);

                        if (!validationResult.IsValid)
                        {
                            _logger.LogWarning("Attempt {Attempt}: Detect reasons validation failed - {Reason}", attempt, validationResult.Reason);

                            if (attempt < maxRetries)
                                continue;

                            reasons = CleanGreekReasons(reasons);
                        }
                    }
                    break;
                }
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Attempt {Attempt}: Failed to parse LLM response", attempt);
                if (attempt >= maxRetries)
                {
                    _logger.LogError("LLM analysis failed after {MaxRetries} attempts, using statistical analysis only", maxRetries);
                }
            }
        }

        // Step 3: Combine statistical analysis with LLM analysis
        var detectionResult = _aiDetectionService.CalculateAiProbability(textAnalysis, llmProbability);

        // Build response with detailed metrics
        return new DetectResponse
        {
            AiProbability = detectionResult.AiProbability,
            Reasons = reasons.Count > 0 ? reasons : GenerateReasonsFromSignals(detectionResult.Signals),
            Confidence = detectionResult.Confidence,
            Summary = detectionResult.Summary,
            Signals = detectionResult.Signals.Select(s => new SignalResponse
            {
                Name = s.Name,
                Value = s.Value,
                AiLikelihood = (int)Math.Round(s.AiLikelihood * 100),
                Interpretation = s.Interpretation
            }).ToList(),
            Stats = new TextStatsResponse
            {
                TotalWords = textAnalysis.TotalWords,
                TotalSentences = textAnalysis.TotalSentences,
                UniqueWords = textAnalysis.UniqueWords,
                VocabularyDiversity = Math.Round(textAnalysis.VocabularyDiversity * 100, 1),
                AverageSentenceLength = Math.Round(textAnalysis.AverageSentenceLength, 1)
            }
        };
    }

    private static List<string> GenerateReasonsFromSignals(List<DetectionSignal> signals)
    {
        return signals
            .Where(s => Math.Abs(s.AiLikelihood - 0.5) > 0.1) // Only significant signals
            .OrderByDescending(s => Math.Abs(s.AiLikelihood - 0.5))
            .Take(4)
            .Select(s => s.Interpretation)
            .ToList();
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

    private static List<string> CleanGreekReasons(List<string> reasons)
    {
        var cleanedReasons = new List<string>();

        foreach (var reason in reasons)
        {
            var cleaned = CleanGreekText(reason);

            // Only keep reasons that still have meaningful content after cleaning
            if (!string.IsNullOrWhiteSpace(cleaned) && cleaned.Length >= 10)
            {
                cleanedReasons.Add(cleaned);
            }
        }

        // If all reasons were removed, add a generic one in Greek
        if (cleanedReasons.Count == 0)
        {
            cleanedReasons.Add("Το κείμενο αναλύθηκε για χαρακτηριστικά AI");
        }

        return cleanedReasons;
    }

    private string? GenerateHumanizationHints(AiDetectionResult detection, TextAnalysisResult analysis)
    {
        // Only generate hints if AI probability is significant
        if (detection.AiProbability < 30)
            return null;

        var hints = new List<string>();
        var isGreek = analysis.IsGreekText;

        // Get signals with high AI likelihood (sorted by significance)
        var problematicSignals = detection.Signals
            .Where(s => s.AiLikelihood > 0.55)
            .OrderByDescending(s => s.AiLikelihood)
            .Take(4)
            .ToList();

        foreach (var signal in problematicSignals)
        {
            var hint = GetHintForSignal(signal.Name, signal.AiLikelihood, isGreek);
            if (!string.IsNullOrEmpty(hint))
                hints.Add(hint);
        }

        if (hints.Count == 0)
            return null;

        return string.Join("\n", hints.Select((h, i) => $"{i + 1}. {h}"));
    }

    private static string? GetHintForSignal(string signalName, double aiLikelihood, bool isGreek)
    {
        // Map signal names to specific humanization instructions
        var intensity = aiLikelihood > 0.7 ? "CRITICAL" : "Important";

        return signalName switch
        {
            // Universal signals
            "Burstiness" or "Εκρηκτικότητα" => isGreek
                ? $"[{intensity}] Οι προτάσεις έχουν πολύ ομοιόμορφο μήκος. Ανάμειξε μικρές (5-10 λέξεις) με μεγαλύτερες (20-30 λέξεις)."
                : $"[{intensity}] Sentences are too uniform in length. Mix short (5-10 words) with longer ones (20-30 words).",

            "Vocabulary Diversity" or "Ποικιλία Λεξιλογίου" => isGreek
                ? $"[{intensity}] Περιορισμένο λεξιλόγιο. Χρησιμοποίησε συνώνυμα και πιο ποικίλες εκφράσεις."
                : $"[{intensity}] Limited vocabulary. Use synonyms and more varied expressions.",

            "Sentence Variance" or "Διακύμανση Προτάσεων" => isGreek
                ? $"[{intensity}] Οι προτάσεις είναι πολύ ομοιόμορφες. Δημιούργησε μεγαλύτερη ποικιλία στη δομή."
                : $"[{intensity}] Sentences are too uniform. Create more variety in structure.",

            "Repetition" or "Επανάληψη" => isGreek
                ? $"[{intensity}] Εντοπίστηκαν επαναλαμβανόμενες φράσεις. Αντικατέστησε με διαφορετικές διατυπώσεις."
                : $"[{intensity}] Repetitive phrases detected. Replace with different phrasings.",

            "Starter Diversity" or "Ποικιλία Αρχών" => isGreek
                ? $"[{intensity}] Οι προτάσεις ξεκινούν με τον ίδιο τρόπο. Ποικίλε τις αρχές των προτάσεων."
                : $"[{intensity}] Sentences start the same way. Vary sentence beginnings.",

            "Contractions" => isGreek
                ? null // Not applicable for Greek
                : $"[{intensity}] Text lacks natural contractions. Use don't, won't, can't, it's where appropriate.",

            // Greek formal text signals
            "Γενικές Φράσεις" => $"[{intensity}] Αφαίρεσε γενικές φράσεις όπως 'σύμφωνα με έρευνες'. Χρησιμοποίησε συγκεκριμένες αναφορές ή αφαίρεσέ τες.",

            "Εισαγωγικές Φράσεις" => $"[{intensity}] Αφαίρεσε φράσεις όπως 'Είναι σημαντικό να σημειωθεί'. Γράψε απευθείας.",

            "Μεταβατικές Φράσεις" => $"[{intensity}] Υπερβολική χρήση μεταβατικών (επιπλέον, συνεπώς). Χρησιμοποίησε πιο φυσικές συνδέσεις ή καθόλου.",

            "Υπερ-επισημότητα" => $"[{intensity}] Υπερβολικά επίσημο ύφος. Αντικατέστησε λέξεις όπως 'καίριος', 'θεμελιώδης' με απλούστερες.",

            // Greek informal text signals (inverted - low values are problematic)
            "Filler Words" => $"[{intensity}] Το κείμενο είναι πολύ 'καθαρό'. Πρόσθεσε φυσικά filler words όπως 'δηλαδή', 'βασικά' όπου ταιριάζει.",

            "Συντομεύσεις" => $"[{intensity}] Καμία συντόμευση. Για casual ύφος, πρόσθεσε κάποιες (πχ, κλπ, κτλ).",

            "Καθομιλουμένη" => $"[{intensity}] Λείπει η καθομιλουμένη. Χρησιμοποίησε πιο φυσικές εκφράσεις.",

            "Προσωπικές Αντωνυμίες" => isGreek
                ? $"[{intensity}] Απρόσωπο ύφος. Χρησιμοποίησε περισσότερες προσωπικές αντωνυμίες (εγώ, εμείς, μου)."
                : $"[{intensity}] Impersonal style. Use more personal pronouns (I, we, my).",

            "Word Length Variance" => isGreek
                ? $"[{intensity}] Ομοιόμορφο μήκος λέξεων. Χρησιμοποίησε μίγμα μικρών και μεγάλων λέξεων."
                : $"[{intensity}] Uniform word lengths. Use a mix of short and long words.",

            _ => null
        };
    }
}
