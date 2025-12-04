using TextHumanizer.UI.Models;

namespace TextHumanizer.UI.Services;

public interface IApiService
{
    Task<HumanizeResponse?> HumanizeAsync(HumanizeRequest request);
    Task<DetectResponse?> DetectAsync(DetectRequest request);
}
