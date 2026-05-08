using KnowledgeOps.AI;
using KnowledgeOps.Domain.Data;
using KnowledgeOps.Domain.Repositories;
using KnowledgeOps.Web.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));

builder.Services.AddControllersWithViews();
builder.Services.AddKnowledgeOpsAI(builder.Configuration);
builder.Services.AddScoped<ICopilotService, CopilotService>();
builder.Services.AddSingleton<IDocumentRepository, InMemoryDocumentRepository>();
builder.Services.AddScoped<IBusinessRequestRepository, InMemoryBusinessRequestRepository>();

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

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
