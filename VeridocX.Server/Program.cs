using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using VeridocX.Server.Data;
using VeridocX.Server.Endpoints;
using VeridocX.Server.Security;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddNpgsqlDbContext<VeridocXDbContext>(
    connectionName: "Supabase",
    configureDbContextOptions: options => options.UseSnakeCaseNamingConvention());

builder.Services.AddProblemDetails();

builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddSingleton<IIdFingerprinter, HmacIdFingerprinter>();

builder.Services.AddOpenApi();

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

var api = app.MapGroup("/api");

api.MapGet("ping", () => new PingResponse("veridocx-api", "ok", DateTimeOffset.UtcNow))
    .WithName("Ping");

api.MapSaIdEndpoints();

api.MapAffordabilityEndpoints();

app.MapDefaultEndpoints();

app.UseFileServer();

app.Run();

internal record PingResponse(string Service, string Status, DateTimeOffset Time);
