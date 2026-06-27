using System.Text.Json.Serialization;
using Azure;
using Azure.AI.DocumentIntelligence;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using VeridocX.Server.Data;
using VeridocX.Server.Endpoints;
using VeridocX.Server.Security;
using VeridocX.Server.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddNpgsqlDbContext<VeridocXDbContext>(
    connectionName: "Supabase",
    configureDbContextOptions: options => options.UseSnakeCaseNamingConvention());

builder.Services.AddProblemDetails();

builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddSingleton<IIdFingerprinter, HmacIdFingerprinter>();

builder.Services.AddSingleton(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var endpoint = config["Azure:DocumentIntelligence:Endpoint"];
    var key = config["Azure:DocumentIntelligence:Key"];

    if (string.IsNullOrWhiteSpace(endpoint) || string.IsNullOrWhiteSpace(key))
        throw new InvalidOperationException(
            "Azure:DocumentIntelligence:Endpoint and :Key must be configured (user-secrets / Key Vault).");

    return new DocumentIntelligenceClient(new Uri(endpoint), new AzureKeyCredential(key));
});

builder.Services.AddScoped<IIdDocumentExtractor, IdDocumentExtractor>();

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
