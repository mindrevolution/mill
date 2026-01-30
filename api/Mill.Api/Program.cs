using Mill.Api.Endpoints;
using Mill.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Services
builder.Services.AddSingleton<MillService>();
builder.Services.AddOpenApi();

// CORS for local Tauri app
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Middleware
app.UseCors();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Health check
app.MapGet("/api/health", () => Results.Ok(new { status = "ok", version = "0.1.0" }))
   .WithName("Health")
   .WithTags("System");

// Endpoints
app.MapProjectEndpoints();
app.MapLibraryEndpoints();
app.MapSpecEndpoints();
app.MapRunEndpoints();
app.MapHistoryEndpoints();

app.Run();
