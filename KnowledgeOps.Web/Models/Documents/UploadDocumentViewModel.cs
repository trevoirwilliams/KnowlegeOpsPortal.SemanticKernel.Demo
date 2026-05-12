using System;
using System.ComponentModel.DataAnnotations;

namespace KnowledgeOps.Web.Models.Documents;

public class UploadDocumentViewModel
{
    [Required]
    [Display(Name = "PDF document")]
    public IFormFile? File { get; set; }

    [MaxLength(500)]
    [Display(Name = "Tags")]
    public string? Tags { get; set; }
}
