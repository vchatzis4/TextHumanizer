namespace TextHumanizer.Models;

public class TextAnalysisResult
{
    public double Burstiness { get; set; }
    public double VocabularyDiversity { get; set; }
    public double SentenceLengthVariance { get; set; }
    public double AverageSentenceLength { get; set; }
    public double WordLengthVariance { get; set; }
    public double PunctuationDensity { get; set; }
    public double RepetitionScore { get; set; }
    public double SentenceStarterDiversity { get; set; }
    public double ContractionRatio { get; set; }
    public double PersonalPronounRatio { get; set; }
    public int TotalWords { get; set; }
    public int TotalSentences { get; set; }
    public int UniqueWords { get; set; }

    // Language detection
    public bool IsGreekText { get; set; }
    public bool IsFormalStyle { get; set; }

    // Greek-specific metrics (formal text - AI patterns)
    public double GreekGenericPhrasesRatio { get; set; }      // "σύμφωνα με έρευνες", "πολλοί ειδικοί"
    public double GreekTransitionalRepetition { get; set; }   // Repeated "Επιπλέον", "Επιπροσθέτως"
    public double GreekHedgingPhrasesRatio { get; set; }      // "Είναι σημαντικό να", "Αξίζει να σημειωθεί"
    public double GreekOverformalityScore { get; set; }       // Υπερβολικά επίσημο ύφος

    // Greek-specific metrics (informal text - Human patterns)
    public double GreekFillerWordRatio { get; set; }          // λοιπόν, δηλαδή, τέλος πάντων
    public double GreekAbbreviationRatio { get; set; }        // τέσπα, δλδ, οκ
    public double GreekColloquialScore { get; set; }          // Καθημερινή γλώσσα
}
