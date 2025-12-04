using System.ComponentModel.DataAnnotations;

namespace TextHumanizer.Models.Requests;

public class DetectRequest
{
    [Required]
    [MinLength(1)]
    public required string Text { get; set; }
}
