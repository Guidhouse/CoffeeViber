using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using CoffeeGrindBackend.Services;

namespace CoffeeGrindBackend.Extensions;

public static class ServiceExtensions
{
    public static WebApplicationBuilder AddApplicationServices(this WebApplicationBuilder builder)
    {
        // Enforce body size limit at transport level before any buffering occurs
        builder.WebHost.ConfigureKestrel(options =>
        {
            options.Limits.MaxRequestBodySize = 11 * 1024 * 1024; // 11 MB ceiling; app rejects above 10 MB
        });

        builder.Services.AddOpenApi();

        builder.Services.AddCors(options =>
        {
            options.AddPolicy("Frontend", policy =>
            {
                policy.WithOrigins("http://localhost:3001", "https://localhost:3001")
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });
        });

        // Rate limit: max 10 uploads per IP per minute
        builder.Services.AddRateLimiter(options =>
        {
            options.AddFixedWindowLimiter("upload", limiter =>
            {
                limiter.Window = TimeSpan.FromMinutes(1);
                limiter.PermitLimit = 10;
                limiter.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                limiter.QueueLimit = 0;
            });
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        });

        builder.Services.AddSingleton<ImageValidationService>();

        return builder;
    }

    public static WebApplication ConfigureMiddleware(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        app.UseHttpsRedirection();
        app.UseCors("Frontend");
        app.UseRateLimiter();

        return app;
    }
}
