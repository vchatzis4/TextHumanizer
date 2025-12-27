using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TextHumanizer.Interfaces;
using TextHumanizer.Models.Requests;
using TextHumanizer.Models.Responses;

namespace TextHumanizer.Controllers;

[ApiController]
[Route("api")]
public class TextController : ControllerBase
{
    private readonly ILlmService _llmService;
    private readonly ILogger<TextController> _logger;

    public TextController(ILlmService llmService, ILogger<TextController> logger)
    {
        _llmService = llmService;
        _logger = logger;
    }

    [HttpPost("humanize")]
    [EnableRateLimiting("humanize")]
    [ProducesResponseType(typeof(HumanizeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<HumanizeResponse>> Humanize(
        [FromBody] HumanizeRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Received humanize request with tone: {Tone}", request.Tone);

        var response = await _llmService.HumanizeTextAsync(request, cancellationToken);
        return Ok(response);
    }

    [HttpPost("detect")]
    [EnableRateLimiting("detect")]
    [ProducesResponseType(typeof(DetectResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<DetectResponse>> Detect(
        [FromBody] DetectRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Received detect request");

        var response = await _llmService.DetectAiTextAsync(request, cancellationToken);
        return Ok(response);
    }
}
