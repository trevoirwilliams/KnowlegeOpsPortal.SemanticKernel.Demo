using KnowledgeOps.AI;
using KnowledgeOps.Domain.Data;
using KnowledgeOps.Domain.Models;
using KnowledgeOps.Domain.Repositories;
using KnowledgeOps.Web.BackgroundWorkers;
using KnowledgeOps.Web.Models.Copilot;
using KnowledgeOps.Web.Models.Documents;
using KnowledgeOps.Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;

    options.Password.RequiredLength = 8;
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();
builder.Services.AddSqliteVectorStore(_ => connectionString);

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/Login";
});

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = options.DefaultPolicy;
});

string? ironPdfLicenseKey = builder.Configuration["IronPdf:LicenseKey"];

if (!string.IsNullOrWhiteSpace(ironPdfLicenseKey))
{
    IronPdf.License.LicenseKey = ironPdfLicenseKey;
}

string? ironOcrLicenseKey = builder.Configuration["IronOcr:LicenseKey"];

if (!string.IsNullOrWhiteSpace(ironOcrLicenseKey))
{
    IronOcr.License.LicenseKey = ironOcrLicenseKey;
}

builder.Services.AddControllersWithViews();
builder.Services.AddKnowledgeOpsAI(builder.Configuration);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<ICopilotService, CopilotService>();
builder.Services.AddScoped<ICopilotConversationService, CopilotConversationService>();
builder.Services.AddScoped<IDocumentUploadService, DocumentUploadService>();
builder.Services.AddScoped<IDocumentProcessingService, DocumentProcessingService>();
builder.Services.AddScoped<IDocumentChunkingService, DocumentChunkingService>();
builder.Services.AddScoped<IDocumentEmbeddingService, DocumentEmbeddingService>();
builder.Services.AddScoped<IDocumentRetrievalService, DocumentRetrievalService>();

builder.Services.AddScoped<ICopilotHistorySummarizerService, CopilotHistorySummarizerService>();

builder.Services.AddHostedService<CopilotHistoryCleanupWorkerService>();
builder.Services.AddHostedService<DocumentProcessingWorkerService>();
builder.Services.AddHostedService<DocumentChunkingWorkerService>();
builder.Services.AddHostedService<DocumentEmbeddingWorkerService>();

builder.Services
    .AddOptions<CopilotHistoryOptions>()
    .Bind(builder.Configuration.GetSection(CopilotHistoryOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services
    .AddOptions<DocumentUploadOptions>()
    .Bind(builder.Configuration.GetSection(DocumentUploadOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services
    .AddOptions<DocumentChunkingOptions>()
    .Bind(builder.Configuration.GetSection(DocumentChunkingOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets().AllowAnonymous();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
