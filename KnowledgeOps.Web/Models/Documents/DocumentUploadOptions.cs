using System;
using System.ComponentModel.DataAnnotations;

namespace KnowledgeOps.Web.Models.Documents;

public class DocumentUploadOptions
{
    public const string SectionName = "DocumentUpload";

    [Required]
    public string StoragePath { get; set; } = "App_Data/Uploads/Documents";

    [Range(1, long.MaxValue)]
    public long MaxFileSizeBytes { get; set; } = 10 * 1024 * 1024;

    public string[] AllowedContentTypes { get; set; } = ["application/pdf"];

    public string[] AllowedExtensions { get; set; } = [".pdf"];
}