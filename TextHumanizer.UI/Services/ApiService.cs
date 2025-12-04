using System.Net.Http.Json;
using TextHumanizer.UI.Models;

namespace TextHumanizer.UI.Services;

public class ApiService : IApiService
{
    private readonly HttpClient _httpClient;

    public ApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<HumanizeResponse?> HumanizeAsync(HumanizeRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync("api/humanize", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<HumanizeResponse>();
    }

    public async Task<DetectResponse?> DetectAsync(DetectRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync("api/detect", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<DetectResponse>();
    }
}
