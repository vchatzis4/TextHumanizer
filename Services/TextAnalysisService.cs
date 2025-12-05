using System.Text.RegularExpressions;
using TextHumanizer.Interfaces;
using TextHumanizer.Models;

namespace TextHumanizer.Services;

public class TextAnalysisService : ITextAnalysisService
{
    public TextAnalysisResult Analyze(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return new TextAnalysisResult();

        var sentences = SplitIntoSentences(text);
        var words = ExtractWords(text);

        return new TextAnalysisResult
        {
            // Burstiness - variance in sentence length (humans are more variable)
            Burstiness = CalculateBurstiness(sentences),

            // Vocabulary diversity - unique words ratio (humans often more diverse)
            VocabularyDiversity = CalculateVocabularyDiversity(words),

            // Sentence length variance
            SentenceLengthVariance = CalculateSentenceLengthVariance(sentences),

            // Average sentence length
            AverageSentenceLength = CalculateAverageSentenceLength(sentences),

            // Word length variance (humans use more varied word lengths)
            WordLengthVariance = CalculateWordLengthVariance(words),

            // Punctuation density
            PunctuationDensity = CalculatePunctuationDensity(text),

            // Repetition score (AI tends to repeat phrases)
            RepetitionScore = CalculateRepetitionScore(text, words),

            // Sentence starter diversity
            SentenceStarterDiversity = CalculateSentenceStarterDiversity(sentences),

            // Contraction usage (humans use more contractions)
            ContractionRatio = CalculateContractionRatio(text, words),

            // Personal pronoun usage
            PersonalPronounRatio = CalculatePersonalPronounRatio(words),

            TotalWords = words.Count,
            TotalSentences = sentences.Count,
            UniqueWords = words.Distinct(StringComparer.OrdinalIgnoreCase).Count()
        };
    }

    private List<string> SplitIntoSentences(string text)
    {
        // Split on sentence-ending punctuation
        var sentences = Regex.Split(text, @"(?<=[.!?])\s+")
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToList();
        return sentences;
    }

    private List<string> ExtractWords(string text)
    {
        return Regex.Matches(text, @"\b[a-zA-Z']+\b")
            .Select(m => m.Value)
            .ToList();
    }

    private double CalculateBurstiness(List<string> sentences)
    {
        if (sentences.Count < 2) return 0;

        var lengths = sentences.Select(s => ExtractWords(s).Count).ToList();
        var mean = lengths.Average();
        var variance = lengths.Sum(l => Math.Pow(l - mean, 2)) / lengths.Count;
        var stdDev = Math.Sqrt(variance);

        // Burstiness = (stdDev - mean) / (stdDev + mean)
        // Range: -1 to 1, higher = more variable (more human-like)
        if (stdDev + mean == 0) return 0;
        return (stdDev - mean) / (stdDev + mean);
    }

    private double CalculateVocabularyDiversity(List<string> words)
    {
        if (words.Count == 0) return 0;

        var uniqueWords = words.Distinct(StringComparer.OrdinalIgnoreCase).Count();
        // Type-Token Ratio (TTR)
        return (double)uniqueWords / words.Count;
    }

    private double CalculateSentenceLengthVariance(List<string> sentences)
    {
        if (sentences.Count < 2) return 0;

        var lengths = sentences.Select(s => ExtractWords(s).Count).ToList();
        var mean = lengths.Average();
        return lengths.Sum(l => Math.Pow(l - mean, 2)) / lengths.Count;
    }

    private double CalculateAverageSentenceLength(List<string> sentences)
    {
        if (sentences.Count == 0) return 0;
        return sentences.Average(s => ExtractWords(s).Count);
    }

    private double CalculateWordLengthVariance(List<string> words)
    {
        if (words.Count < 2) return 0;

        var lengths = words.Select(w => w.Length).ToList();
        var mean = lengths.Average();
        return lengths.Sum(l => Math.Pow(l - mean, 2)) / lengths.Count;
    }

    private double CalculatePunctuationDensity(string text)
    {
        if (string.IsNullOrEmpty(text)) return 0;

        var punctuationCount = text.Count(c => char.IsPunctuation(c));
        return (double)punctuationCount / text.Length;
    }

    private double CalculateRepetitionScore(string text, List<string> words)
    {
        if (words.Count < 10) return 0;

        // Check for repeated n-grams (2-grams and 3-grams)
        var bigrams = new List<string>();
        var trigrams = new List<string>();

        for (int i = 0; i < words.Count - 1; i++)
        {
            bigrams.Add($"{words[i].ToLower()} {words[i + 1].ToLower()}");
            if (i < words.Count - 2)
            {
                trigrams.Add($"{words[i].ToLower()} {words[i + 1].ToLower()} {words[i + 2].ToLower()}");
            }
        }

        var bigramRepetition = bigrams.Count > 0
            ? 1.0 - ((double)bigrams.Distinct().Count() / bigrams.Count)
            : 0;
        var trigramRepetition = trigrams.Count > 0
            ? 1.0 - ((double)trigrams.Distinct().Count() / trigrams.Count)
            : 0;

        // Higher score = more repetition (more AI-like)
        return (bigramRepetition + trigramRepetition * 2) / 3;
    }

    private double CalculateSentenceStarterDiversity(List<string> sentences)
    {
        if (sentences.Count < 2) return 1;

        var starters = sentences
            .Select(s => ExtractWords(s).FirstOrDefault()?.ToLower() ?? "")
            .Where(s => !string.IsNullOrEmpty(s))
            .ToList();

        if (starters.Count == 0) return 1;

        // Unique starters / total starters
        return (double)starters.Distinct().Count() / starters.Count;
    }

    private double CalculateContractionRatio(string text, List<string> words)
    {
        if (words.Count == 0) return 0;

        var contractions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "i'm", "you're", "he's", "she's", "it's", "we're", "they're",
            "i've", "you've", "we've", "they've",
            "i'll", "you'll", "he'll", "she'll", "it'll", "we'll", "they'll",
            "i'd", "you'd", "he'd", "she'd", "it'd", "we'd", "they'd",
            "isn't", "aren't", "wasn't", "weren't", "haven't", "hasn't", "hadn't",
            "won't", "wouldn't", "don't", "doesn't", "didn't", "can't", "couldn't",
            "shouldn't", "mightn't", "mustn't", "let's", "that's", "who's",
            "what's", "here's", "there's", "where's", "when's", "why's", "how's"
        };

        var contractionCount = words.Count(w => contractions.Contains(w));
        return (double)contractionCount / words.Count;
    }

    private double CalculatePersonalPronounRatio(List<string> words)
    {
        if (words.Count == 0) return 0;

        var pronouns = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "i", "me", "my", "mine", "myself",
            "you", "your", "yours", "yourself",
            "he", "him", "his", "himself",
            "she", "her", "hers", "herself",
            "we", "us", "our", "ours", "ourselves",
            "they", "them", "their", "theirs", "themselves"
        };

        var pronounCount = words.Count(w => pronouns.Contains(w));
        return (double)pronounCount / words.Count;
    }
}
