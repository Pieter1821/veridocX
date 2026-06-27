var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddProblemDetails();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}


var api = app.MapGroup("/api");

// Temporary connectivity probe so the React console can prove it reaches the API.
// Phase 0.5 replaces this with the real document upload + status endpoints.
api.MapGet("ping", () => new PingResponse("veridocx-api", "ok", DateTimeOffset.UtcNow))
    .WithName("Ping");

app.MapDefaultEndpoints();

app.UseFileServer();

app.Run();

internal record PingResponse(string Service, string Status, DateTimeOffset Time);
