using System.Text.RegularExpressions;
using TextHumanizer.Interfaces;
using TextHumanizer.Models;

namespace TextHumanizer.Services;

public class TextAnalysisService : ITextAnalysisService
{
    // Greek AI patterns - formal text (these suggest AI-generated content)
    private static readonly string[] GreekGenericPhrases =
    {
        "σύμφωνα με έρευνες", "σύμφωνα με μελέτες", "σύμφωνα με ειδικούς",
        "πολλοί ειδικοί", "πολλοί επιστήμονες", "πολλές μελέτες",
        "είναι γνωστό ότι", "είναι ευρέως γνωστό", "είναι κοινώς αποδεκτό",
        "έχει αποδειχθεί ότι", "έρευνες δείχνουν", "μελέτες αποδεικνύουν",
        "όπως είναι γνωστό", "ως γνωστόν", "κατά γενική ομολογία"
    };

    private static readonly string[] GreekHedgingPhrases =
    {
        "είναι σημαντικό να", "αξίζει να σημειωθεί", "αξίζει να αναφερθεί",
        "πρέπει να τονιστεί", "είναι απαραίτητο να", "κρίνεται σκόπιμο",
        "θα πρέπει να επισημανθεί", "δεν πρέπει να παραβλέψουμε",
        "είναι αναγκαίο να", "καθίσταται σαφές ότι", "συνεπάγεται ότι"
    };

    private static readonly string[] GreekTransitionalPhrases =
    {
        "επιπλέον", "επιπροσθέτως", "εξάλλου", "ωστόσο", "παρόλα αυτά",
        "εντούτοις", "συνεπώς", "κατά συνέπεια", "εν κατακλείδι",
        "εν συμπεράσματι", "συμπερασματικά", "τέλος", "αφενός", "αφετέρου",
        "πρωτίστως", "δευτερευόντως", "καταρχάς", "καταρχήν"
    };

    private static readonly string[] GreekOverformalWords =
    {
        "καίριος", "καίρια", "ουσιώδης", "θεμελιώδης", "αναπόσπαστος",
        "αδιαμφισβήτητος", "αναμφισβήτητα", "αναντίρρητα", "εξαιρετικά",
        "ιδιαιτέρως", "εξόχως", "υπερβαλλόντως", "διαρκώς", "συνεχώς",
        "απαραιτήτως", "οπωσδήποτε", "αναμφιβόλως", "προφανώς"
    };

    // Greek human patterns - informal text (these suggest human-written content)
    private static readonly string[] GreekFillerWords =
    {
        "λοιπόν", "δηλαδή", "τέλος πάντων", "καλά", "εντάξει", "οκ", "οκει",
        "ξέρεις", "ξέρω γω", "τι να πω", "πώς να το πω", "κοίτα", "κοιτάξτε",
        "άκου", "ακούστε", "βασικά", "ουσιαστικά", "πες", "για πες",
        "ε", "εμ", "χμ", "α", "ω"
    };

    private static readonly string[] GreekAbbreviations =
    {
        "τέσπα", "δλδ", "κλπ", "πχ", "χχ", "λολ", "ομγ", "μπλμπλα",
        "οκ", "btw", "tbh", "omg", "lol", "κτλ"
    };

    private static readonly string[] GreekColloquialExpressions =
    {
        "ρε", "μωρέ", "βρε", "παιδιά", "φίλε", "φίλοι", "μάγκες",
        "τι λες", "τι λέτε", "πάμε", "έλα", "ελάτε", "άντε", "αντε",
        "τέλεια", "ωραία", "μια χαρά", "καλώς", "ναι ρε", "όχι ρε",
        "σοβαρά", "αλήθεια", "στα αλήθεια", "για δες", "κριντζ", "χαλαρά"
    };

    // Greek personal pronouns
    private static readonly HashSet<string> GreekPersonalPronouns = new(StringComparer.OrdinalIgnoreCase)
    {
        "εγώ", "εμένα", "μου", "με", "εσύ", "εσένα", "σου", "σε",
        "αυτός", "αυτή", "αυτό", "αυτού", "αυτής", "του", "της", "τον", "την", "το",
        "εμείς", "εμάς", "μας", "εσείς", "εσάς", "σας",
        "αυτοί", "αυτές", "αυτά", "αυτών", "τους", "τις", "τα"
    };

    public TextAnalysisResult Analyze(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return new TextAnalysisResult();

        var sentences = SplitIntoSentences(text);
        var isGreek = IsGreekText(text);
        var words = isGreek ? ExtractGreekWords(text) : ExtractWords(text);
        var lowerText = text.ToLower();

        var result = new TextAnalysisResult
        {
            // Basic metrics
            Burstiness = CalculateBurstiness(sentences, isGreek),
            VocabularyDiversity = CalculateVocabularyDiversity(words),
            SentenceLengthVariance = CalculateSentenceLengthVariance(sentences, isGreek),
            AverageSentenceLength = CalculateAverageSentenceLength(sentences, isGreek),
            WordLengthVariance = CalculateWordLengthVariance(words),
            PunctuationDensity = CalculatePunctuationDensity(text),
            RepetitionScore = CalculateRepetitionScore(text, words),
            SentenceStarterDiversity = CalculateSentenceStarterDiversity(sentences, isGreek),
            ContractionRatio = isGreek ? 0 : CalculateContractionRatio(text, words),
            PersonalPronounRatio = isGreek
                ? CalculateGreekPersonalPronounRatio(words)
                : CalculatePersonalPronounRatio(words),

            TotalWords = words.Count,
            TotalSentences = sentences.Count,
            UniqueWords = words.Distinct(StringComparer.OrdinalIgnoreCase).Count(),

            // Language detection
            IsGreekText = isGreek
        };

        // Calculate Greek-specific metrics
        if (isGreek)
        {
            // Determine formality
            result.IsFormalStyle = DetermineFormality(lowerText, words);

            // AI patterns (formal text signals)
            result.GreekGenericPhrasesRatio = CalculatePhraseRatio(lowerText, GreekGenericPhrases, words.Count);
            result.GreekTransitionalRepetition = CalculateTransitionalRepetition(lowerText, sentences.Count);
            result.GreekHedgingPhrasesRatio = CalculatePhraseRatio(lowerText, GreekHedgingPhrases, words.Count);
            result.GreekOverformalityScore = CalculateOverformalityScore(words);

            // Human patterns (informal text signals)
            result.GreekFillerWordRatio = CalculateWordListRatio(words, GreekFillerWords);
            result.GreekAbbreviationRatio = CalculateWordListRatio(words, GreekAbbreviations);
            result.GreekColloquialScore = CalculateColloquialScore(lowerText, words);
        }

        return result;
    }

    private static bool IsGreekText(string text)
    {
        int greekCount = 0;
        int latinCount = 0;

        foreach (char c in text)
        {
            // Greek Unicode ranges
            if ((c >= 'Α' && c <= 'Ω') || (c >= 'α' && c <= 'ω') ||
                (c >= 'ά' && c <= 'ώ') || c == 'ς')
                greekCount++;
            else if ((c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z'))
                latinCount++;
        }

        return greekCount > latinCount;
    }

    private static bool DetermineFormality(string lowerText, List<string> words)
    {
        var informalCount = 0;
        var formalCount = 0;

        // Check for informal markers
        foreach (var filler in GreekFillerWords)
        {
            if (lowerText.Contains(filler)) informalCount++;
        }
        foreach (var abbr in GreekAbbreviations)
        {
            if (words.Any(w => w.Equals(abbr, StringComparison.OrdinalIgnoreCase))) informalCount++;
        }
        foreach (var colloquial in GreekColloquialExpressions)
        {
            if (lowerText.Contains(colloquial)) informalCount++;
        }

        // Check for formal markers
        foreach (var phrase in GreekHedgingPhrases)
        {
            if (lowerText.Contains(phrase)) formalCount++;
        }
        foreach (var word in GreekOverformalWords)
        {
            if (words.Any(w => w.Equals(word, StringComparison.OrdinalIgnoreCase))) formalCount++;
        }
        foreach (var phrase in GreekTransitionalPhrases)
        {
            if (lowerText.Contains(phrase)) formalCount++;
        }

        return formalCount >= informalCount;
    }

    private List<string> SplitIntoSentences(string text)
    {
        var sentences = Regex.Split(text, @"(?<=[.!?;])\s+")
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

    private List<string> ExtractGreekWords(string text)
    {
        // Match Greek words (including accented characters) and Latin words
        return Regex.Matches(text, @"\b[α-ωάέήίόύώΑ-Ωςa-zA-Z]+\b", RegexOptions.IgnoreCase)
            .Select(m => m.Value)
            .ToList();
    }

    private double CalculateBurstiness(List<string> sentences, bool isGreek)
    {
        if (sentences.Count < 2) return 0;

        var lengths = sentences.Select(s =>
            isGreek ? ExtractGreekWords(s).Count : ExtractWords(s).Count).ToList();

        if (lengths.All(l => l == 0)) return 0;

        var mean = lengths.Average();
        var variance = lengths.Sum(l => Math.Pow(l - mean, 2)) / lengths.Count;
        var stdDev = Math.Sqrt(variance);

        if (stdDev + mean == 0) return 0;
        return (stdDev - mean) / (stdDev + mean);
    }

    private double CalculateVocabularyDiversity(List<string> words)
    {
        if (words.Count == 0) return 0;

        var uniqueWords = words.Distinct(StringComparer.OrdinalIgnoreCase).Count();
        return (double)uniqueWords / words.Count;
    }

    private double CalculateSentenceLengthVariance(List<string> sentences, bool isGreek)
    {
        if (sentences.Count < 2) return 0;

        var lengths = sentences.Select(s =>
            isGreek ? ExtractGreekWords(s).Count : ExtractWords(s).Count).ToList();

        if (lengths.All(l => l == 0)) return 0;

        var mean = lengths.Average();
        return lengths.Sum(l => Math.Pow(l - mean, 2)) / lengths.Count;
    }

    private double CalculateAverageSentenceLength(List<string> sentences, bool isGreek)
    {
        if (sentences.Count == 0) return 0;
        return sentences.Average(s =>
            isGreek ? ExtractGreekWords(s).Count : ExtractWords(s).Count);
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

        return (bigramRepetition + trigramRepetition * 2) / 3;
    }

    private double CalculateSentenceStarterDiversity(List<string> sentences, bool isGreek)
    {
        if (sentences.Count < 2) return 1;

        var starters = sentences
            .Select(s =>
            {
                var words = isGreek ? ExtractGreekWords(s) : ExtractWords(s);
                return words.FirstOrDefault()?.ToLower() ?? "";
            })
            .Where(s => !string.IsNullOrEmpty(s))
            .ToList();

        if (starters.Count == 0) return 1;

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

    private double CalculateGreekPersonalPronounRatio(List<string> words)
    {
        if (words.Count == 0) return 0;

        var pronounCount = words.Count(w => GreekPersonalPronouns.Contains(w));
        return (double)pronounCount / words.Count;
    }

    // Greek-specific analysis methods

    private static double CalculatePhraseRatio(string lowerText, string[] phrases, int wordCount)
    {
        if (wordCount == 0) return 0;

        var matchCount = phrases.Count(phrase => lowerText.Contains(phrase));
        // Normalize by word count (phrases per 100 words)
        return (double)matchCount / wordCount * 100;
    }

    private static double CalculateTransitionalRepetition(string lowerText, int sentenceCount)
    {
        if (sentenceCount == 0) return 0;

        var transitionCount = GreekTransitionalPhrases.Sum(phrase =>
            Regex.Matches(lowerText, Regex.Escape(phrase)).Count);

        // More than 1 transitional phrase per 2 sentences is suspicious
        return (double)transitionCount / sentenceCount;
    }

    private static double CalculateOverformalityScore(List<string> words)
    {
        if (words.Count == 0) return 0;

        var overformalCount = words.Count(w =>
            GreekOverformalWords.Any(of => of.Equals(w, StringComparison.OrdinalIgnoreCase)));

        return (double)overformalCount / words.Count * 100;
    }

    private static double CalculateWordListRatio(List<string> words, string[] wordList)
    {
        if (words.Count == 0) return 0;

        var matchCount = words.Count(w =>
            wordList.Any(item => item.Equals(w, StringComparison.OrdinalIgnoreCase)));

        return (double)matchCount / words.Count;
    }

    private static double CalculateColloquialScore(string lowerText, List<string> words)
    {
        if (words.Count == 0) return 0;

        var colloquialCount = 0;

        // Check for colloquial expressions
        foreach (var expr in GreekColloquialExpressions)
        {
            colloquialCount += Regex.Matches(lowerText, $@"\b{Regex.Escape(expr)}\b").Count;
        }

        // Check for informal punctuation patterns (multiple ! or ?)
        colloquialCount += Regex.Matches(lowerText, @"[!?]{2,}").Count;

        // Check for emoticons/emoji patterns
        colloquialCount += Regex.Matches(lowerText, @"[:;][-']?[)(\[\]DPp]").Count;

        return (double)colloquialCount / words.Count * 100;
    }
}
