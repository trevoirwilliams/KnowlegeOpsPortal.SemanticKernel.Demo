using KnowledgeOps.Domain.Repositories;
using KnowledgeOps.Web.Models.Documents;
using Microsoft.AspNetCore.Mvc;

namespace KnowledgeOps.Web.Controllers;

public class DocumentsController(
    IDocumentRepository documentRepository) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var documents = await documentRepository.GetAllAsync(cancellationToken);

        var model = documents
            .OrderBy(document => document.Title)
            .Select(document => new DocumentListItemViewModel
            {
                Id = document.Id,
                Title = document.Title,
                Category = document.Category,
                Department = document.Department,
                Status = document.Status,
                LastReviewedOn = document.LastReviewedOn,
                Summary = document.Summary
            })
            .ToList();

        return View(model);
    }

    public async Task<IActionResult> Details(string id,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return BadRequest();
        }

        var document = await documentRepository.GetByIdAsync(id, cancellationToken);

        if (document is null)
        {
            return NotFound();
        }

        var model = new DocumentDetailsViewModel
        {
            Id = document.Id,
            Title = document.Title,
            Category = document.Category,
            Department = document.Department,
            Owner = document.Owner,
            Status = document.Status,
            LastReviewedOn = document.LastReviewedOn,
            Summary = document.Summary,
            Tags = document.Tags
        };

        return View(model);
    }
}
