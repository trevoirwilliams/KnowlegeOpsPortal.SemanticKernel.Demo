using KnowledgeOps.Web.Models.Copilot;
using KnowledgeOps.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace KnowledgeOps.Web.Controllers;

public class CopilotController(
ICopilotService copilotService,
ICopilotConversationService conversationService,
ICurrentUserService currentUserService,
IOptions<CopilotHistoryOptions> historyOptions,
ILogger<CopilotController> logger) : Controller
{
    [HttpPost("message")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Message(
        [FromBody] CopilotRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new ValidationProblemDetails(ModelState));
        }

        try
        {
            var response = await copilotService.GetResponseAsync(
                request,
                cancellationToken);

            return Ok(response);
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning("Copilot request was cancelled.");

            return StatusCode(
                StatusCodes.Status499ClientClosedRequest,
                new { message = "The copilot request was cancelled." });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Copilot request failed.");

            return Problem(
                title: "The copilot could not process the request.",
                detail: "An error occurred while generating the copilot response.",
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }
    
    [HttpPost("history")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> History(
        [FromBody] CopilotPageContext? context,
        CancellationToken cancellationToken)
    {
        var settings = historyOptions.Value;

        var history = await conversationService.GetHistoryResponseAsync(
            context,
            currentUserService.UserId,
            TimeSpan.FromHours(settings.ConversationRelevanceHours),
            settings.MaxDisplayMessages,
            cancellationToken);

        return Ok(history);
    }

    [HttpPost("clear-history")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ClearHistory(
        [FromBody] ClearCopilotHistoryRequest request,
        CancellationToken cancellationToken)
    {
        if (request.ConversationId is null)
        {
            return Ok(new { cleared = false });
        }

        var cleared = await conversationService.ClearConversationAsync(
            request.ConversationId.Value,
            currentUserService.UserId,
            cancellationToken);

        return Ok(new { cleared });
    }

}

