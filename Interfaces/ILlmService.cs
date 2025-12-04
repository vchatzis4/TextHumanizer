using TextHumanizer.Models.Requests;
using TextHumanizer.Models.Responses;

namespace TextHumanizer.Interfaces;

public interface ILlmService
{
    Task<HumanizeResponse> HumanizeTextAsync(HumanizeRequest request, CancellationToken cancellationToken = default);
    Task<DetectResponse> DetectAiTextAsync(DetectRequest request, CancellationToken cancellationToken = default);
}
