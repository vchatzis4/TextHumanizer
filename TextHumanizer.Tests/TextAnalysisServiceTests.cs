using TextHumanizer.Services;
using TextHumanizer.Models;

namespace TextHumanizer.Tests;

public class TextAnalysisServiceTests
{
    private readonly TextAnalysisService _service = new();

    // --- Basic tests ---
    [Fact]
    public void Analyze_EmptyString_ReturnsEmptyResult()
    {
        string text = "";

        TextAnalysisResult result = _service.Analyze(text);

        Assert.Equal(0, result.TotalWords);
        Assert.Equal(0, result.TotalSentences);
    }

    [Fact]
    public void Analyze_NullString_ReturnsEmptyResult()
    {
        string? text = null;

        TextAnalysisResult result = _service.Analyze(text!);

        Assert.Equal(0, result.TotalWords);
    }

    [Fact]
    public void Analyze_SingleWord_ReturnsOneWord()
    {
        string text = "Hello";

        TextAnalysisResult result = _service.Analyze(text);

        Assert.Equal(1, result.TotalWords);
        Assert.Equal(1, result.UniqueWords);
    }

    [Fact]
    public void Analyze_SimpleSentence_CountsWordsCorrectly()
    {
        string text = "The quick brown fox jumps.";

        TextAnalysisResult result = _service.Analyze(text);

        Assert.Equal(5, result.TotalWords);
        Assert.Equal(1, result.TotalSentences);
    }

    // --- Language detection ---
    [Fact]
    public void Analyze_EnglishText_DetectsAsNonGreek()
    {
        string text = "This is a simple English sentence.";

        TextAnalysisResult result = _service.Analyze(text);

        Assert.False(result.IsGreekText);
    }

    [Fact]
    public void Analyze_GreekText_DetectsAsGreek()
    {
        string text = "Αυτό είναι ένα απλό ελληνικό κείμενο.";

        TextAnalysisResult result = _service.Analyze(text);

        Assert.True(result.IsGreekText);
    }

    // --- Vocabulary diversity ---
    [Fact]
    public void Analyze_AllUniqueWords_HasHighVocabularyDiversity()
    {
        string text = "The quick brown fox jumps over lazy dog.";

        TextAnalysisResult result = _service.Analyze(text);

        Assert.Equal(1.0, result.VocabularyDiversity);
    }

    [Fact]
    public void Analyze_RepeatedWords_HasLowerVocabularyDiversity()
    {
        string text = "The cat and the dog.";

        TextAnalysisResult result = _service.Analyze(text);

        Assert.Equal(0.8, result.VocabularyDiversity);
    }

    // --- Contractions (English only) ---
    [Fact]
    public void Analyze_TextWithContractions_DetectsContractions()
    {
        string text = "I don't know why I'm here.";

        TextAnalysisResult result = _service.Analyze(text);

        Assert.True(result.ContractionRatio > 0);
    }

    [Fact]
    public void Analyze_TextWithoutContractions_HasZeroContractionRatio()
    {
        string text = "I do not know why I am here.";

        TextAnalysisResult result = _service.Analyze(text);

        Assert.Equal(0, result.ContractionRatio);
    }

    // --- Personal pronouns ---
    [Fact]
    public void Analyze_TextWithPronouns_DetectsPronouns()
    {
        string text = "I love my cat.";

        TextAnalysisResult result = _service.Analyze(text);

        Assert.Equal(0.5, result.PersonalPronounRatio);
    }

    // --- Sentence counting ---
    [Fact]
    public void Analyze_MultipleSentences_CountsSentencesCorrectly()
    {
        string text = "First sentence. Second sentence! Third sentence?";

        TextAnalysisResult result = _service.Analyze(text);

        Assert.Equal(3, result.TotalSentences);
    }

    // --- Using [Theory] to test multiple inputs at once ---
    [Theory]
    [InlineData("Hello.", 1)]
    [InlineData("Hello. World.", 2)]
    [InlineData("One! Two! Three!", 3)]
    [InlineData("Question? Answer. Exclaim!", 3)]
    public void Analyze_VariousSentences_CountsCorrectly(string text, int expectedSentences)
    {
        TextAnalysisResult result = _service.Analyze(text);

        Assert.Equal(expectedSentences, result.TotalSentences);
    }

    [Theory]
    [InlineData("cat", 1)]
    [InlineData("cat dog", 2)]
    [InlineData("one two three four five", 5)]
    public void Analyze_VariousWordCounts_CountsCorrectly(string text, int expectedWords)
    {
        TextAnalysisResult result = _service.Analyze(text);

        Assert.Equal(expectedWords, result.TotalWords);
    }
}
