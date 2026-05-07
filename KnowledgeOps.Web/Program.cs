using KnowledgeOps.AI;
using KnowledgeOps.AI.Repositories;
using KnowledgeOps.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
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
