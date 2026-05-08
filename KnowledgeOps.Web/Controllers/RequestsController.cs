using KnowledgeOps.AI.Repositories;
using KnowledgeOps.Web.Models.Requests;
using Microsoft.AspNetCore.Mvc;

namespace KnowledgeOps.Web.Controllers
{
    public class RequestsController(
    IBusinessRequestRepository requestRepository) : Controller
    {
        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var requests = await requestRepository.GetOpenRequestsAsync(cancellationToken);

            var model = requests
                .Select(request => new RequestListItemViewModel
                {
                    Id = request.Id,
                    Title = request.Title,
                    Department = request.Department,
                    RequestedBy = request.RequestedBy,
                    Status = request.Status,
                    Impact = request.Impact,
                    Urgency = request.Urgency,
                    SubmittedOnUtc = request.SubmittedOnUtc,
                    RequiredByUtc = request.RequiredByUtc,
                    AssignedTo = request.AssignedTo
                })
                .ToList();

            return View(model);
        }

        public async Task<IActionResult> Details(
            string id,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return BadRequest();
            }

            var request = await requestRepository.GetByIdAsync(id, cancellationToken);

            if (request is null)
            {
                return NotFound();
            }

            var model = new RequestDetailsViewModel
            {
                Id = request.Id,
                Title = request.Title,
                Department = request.Department,
                RequestedBy = request.RequestedBy,
                Description = request.Description,
                BusinessJustification = request.BusinessJustification,
                Status = request.Status,
                Impact = request.Impact,
                Urgency = request.Urgency,
                SubmittedOnUtc = request.SubmittedOnUtc,
                RequiredByUtc = request.RequiredByUtc,
                AssignedTo = request.AssignedTo
            };

            return View(model);
        }
    }
}
