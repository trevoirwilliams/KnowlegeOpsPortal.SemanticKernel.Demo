using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using KnowledgeOps.Web.Models;
using KnowledgeOps.AI.Services;
using KnowledgeOps.Web.Services;
using KnowledgeOps.Web.Models.Copilot;

namespace KnowledgeOps.Web.Controllers;

public class HomeController(ICopilotService copilotService) : Controller
{
    public IActionResult Index()
    {
        return View();
    }

    public async Task<IActionResult> CopilotSmokeTest(CancellationToken cancellationToken)
    {
        var response = await copilotService.GetResponseAsync(
            new CopilotRequest
            {
                Message = "What can you help me with on this page?",
                Context = new CopilotPageContext
                {
                    Area = "Home",
                    PageTitle = "KnowledgeOps Portal Home",
                    Summary = "The user is viewing the portal landing page."
                }
            },
            cancellationToken);

        return Content(response.Message);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
