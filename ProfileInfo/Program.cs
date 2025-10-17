using ProfileInfo.Services;
using ProfileInfo.Services.Interfaces;
using Polly;
using Microsoft.AspNetCore.RateLimiting;

var builder = WebApplication.CreateBuilder(args); //1

var catFactApiTimeout = builder.Configuration.GetValue<int>("CatFactApi:TimeoutSeconds", 10);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var retryPolicy = Policy<HttpResponseMessage>
    .Handle<HttpRequestException>()
    .Or<TaskCanceledException>()
    .WaitAndRetryAsync(
        retryCount: 3,
        sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
        onRetry: (outcome, timespan, retryCount, context) =>
        {
            Console.WriteLine($"Retry {retryCount} after {timespan.TotalSeconds} seconds due to {outcome.Exception?.Message}");
        });

var circuitBreakerPolicy = Policy<HttpResponseMessage>
    .Handle<HttpRequestException>()
    .OrResult(r => !r.IsSuccessStatusCode)
    .CircuitBreakerAsync(
        handledEventsAllowedBeforeBreaking: 5,
        durationOfBreak: TimeSpan.FromSeconds(30),
        onBreak: (outcome, timespan) =>
        {
            Console.WriteLine($"Circuit breaker opened for {timespan.TotalSeconds} seconds");
        },
        onReset: () =>
        {
            Console.WriteLine("Circuit breaker reset.");
        });

var timeoutPolicy = Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(catFactApiTimeout));
var combinedPolicy = Policy.WrapAsync(retryPolicy, circuitBreakerPolicy, timeoutPolicy);

builder.Services.AddHttpClient<ICatFactService, CatFactService>()
    .AddPolicyHandler(combinedPolicy)
    .ConfigureHttpClient(client =>
    {
        client.Timeout = TimeSpan.FromSeconds(catFactApiTimeout + 5);
        client.DefaultRequestHeaders.Add("User-Agent", "ProfileApi/1.0");
    });

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", corsBuilder =>
    {
        corsBuilder.AllowAnyOrigin()
                   .AllowAnyMethod()
                   .AllowAnyHeader();
    });
});

builder.Services.AddRateLimiter(rateLimiterOptions =>
{
    rateLimiterOptions.AddFixedWindowLimiter(policyName: "fixed", options =>
    {
        options.PermitLimit = 100;
        options.Window = TimeSpan.FromMinutes(1);
    });
});

builder.Services.AddLogging(config =>
{
    config.ClearProviders();
    config.AddConsole();
    config.SetMinimumLevel(LogLevel.Information);
});

var app = builder.Build();


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseDeveloperExceptionPage();
}

app.UseRateLimiter();

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthorization();

app.Use(async (context, next) =>
{
    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("Request: {Method} {Path}", context.Request.Method, context.Request.Path);
    await next();
});

app.MapControllers();

app.Run();

