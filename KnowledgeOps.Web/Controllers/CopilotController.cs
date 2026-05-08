using KnowledgeOps.Web.Models.Copilot;
using KnowledgeOps.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace KnowledgeOps.Web.Controllers
{
    public class CopilotController(
    ICopilotService copilotService,
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
    }
}
