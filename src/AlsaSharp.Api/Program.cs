using AlsaSharp.Api.Extensions;
using AlsaSharp.Library.Operations.Models;
using AlsaSharp.Library.Operations.Services;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// Configure to bind to http://localhost:5000
builder.WebHost.UseUrls("http://localhost:5000");

// Configure logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Information);

// Add services to the container
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "AlsaSharp API", Version = "v1" });
});

// Add CORS
var corsOrigins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>() 
    ?? ["http://localhost:3000", "http://localhost:5173"];
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(corsOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Add custom services
builder.Services.AddOperations();
builder.Services.AddApiServices();

// Add problem details
builder.Services.AddProblemDetails();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment() || builder.Configuration.GetValue<bool>("Swagger:Enabled"))
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();

// Global exception handler
app.UseExceptionHandler(exceptionHandlerApp =>
{
    exceptionHandlerApp.Run(async context =>
    {
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/problem+json";
        
        var exceptionHandlerFeature = context.Features.Get<IExceptionHandlerFeature>();
        var exception = exceptionHandlerFeature?.Error;
        
        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "An error occurred",
            Detail = exception?.Message
        };
        
        await context.Response.WriteAsJsonAsync(problemDetails);
    });
});

// Map endpoints

app.MapPost("/api/loopback-test", async (
    LoopbackTestRequest request,
    ILoopbackTestOperation operation,
    CancellationToken cancellationToken) =>
{
    var result = await operation.ExecuteAsync(request, cancellationToken);
    return result.Success ? Results.Ok(result) : Results.BadRequest(result);
})
.WithName("LoopbackTest")
.WithTags("Tests");

app.MapPost("/api/snr-test", async (
    SNRTestRequest request,
    ISNRTestOperation operation,
    CancellationToken cancellationToken) =>
{
    var result = await operation.ExecuteAsync(request, cancellationToken);
    return result.Success ? Results.Ok(result) : Results.BadRequest(result);
})
.WithName("SNRTest")
.WithTags("Tests");

app.MapPost("/api/test-tone", async (
    TestToneRequest request,
    ITestToneOperation operation,
    CancellationToken cancellationToken) =>
{
    var result = await operation.ExecuteAsync(request, cancellationToken);
    return result.Success ? Results.Ok(result) : Results.BadRequest(result);
})
.WithName("TestTone")
.WithTags("Tests");

app.MapPost("/api/audio-levels", async (
    AudioLevelsRequest request,
    IAudioLevelsOperation operation,
    CancellationToken cancellationToken) =>
{
    var result = await operation.ExecuteAsync(request, cancellationToken);
    return result.Success ? Results.Ok(result) : Results.BadRequest(result);
})
.WithName("AudioLevels")
.WithTags("Audio");

app.MapPost("/api/copy-only", async (
    CopyOnlyRequest request,
    ICopyOnlyOperation operation,
    CancellationToken cancellationToken) =>
{
    var result = await operation.ExecuteAsync(request, cancellationToken);
    return result.Success ? Results.Ok(result) : Results.BadRequest(result);
})
.WithName("CopyOnly")
.WithTags("Audio");

app.MapGet("/api/audio-cards", async (
    IAudioCardsOperation operation,
    CancellationToken cancellationToken) =>
{
    var result = await operation.GetAudioCardsAsync(cancellationToken);
    return Results.Ok(result);
})
.WithName("GetAudioCards")
.WithTags("Audio");

app.MapHealthChecks("/health");

app.Run();
