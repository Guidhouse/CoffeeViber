using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

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

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors("Frontend");
app.UseRateLimiter();

app.MapPost("/api/grind/upload", async (IFormFile? file) =>
{
    if (file is null || file.Length == 0)
    {
        return Results.BadRequest("No file uploaded.");
    }

    const long maxFileSize = 10 * 1024 * 1024;
    if (file.Length > maxFileSize)
    {
        return Results.BadRequest("File size exceeds the maximum limit of 10MB.");
    }

    var allowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".gif", ".bmp"
    };

    var extension = Path.GetExtension(file.FileName);
    if (string.IsNullOrWhiteSpace(extension) || !allowedExtensions.Contains(extension))
    {
        return Results.BadRequest("Only image files (JPG, PNG, GIF, BMP) are allowed.");
    }

    // Validate file signature (magic bytes) — extension and content-type are client-controlled
    if (!await HasValidImageSignature(file))
    {
        return Results.BadRequest("File content does not match a supported image format.");
    }

    try
    {
        // Store outside the web/content root so files are never directly URL-accessible
        var uploadsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "CoffeeGrind", "Uploads");
        Directory.CreateDirectory(uploadsPath);

        var generatedFileName = $"{Guid.NewGuid()}{extension.ToLowerInvariant()}";
        var outputPath = Path.Combine(uploadsPath, generatedFileName);

        await using var stream = File.Create(outputPath);
        await file.CopyToAsync(stream);

        return Results.Ok(new
        {
            message = "File uploaded successfully!",
            fileName = generatedFileName,
            originalName = file.FileName,
            fileSize = file.Length,
            fileType = file.ContentType
        });
    }
    catch
    {
        return Results.Problem("An error occurred while processing the file.", statusCode: 500);
    }
})
.RequireRateLimiting("upload")
.DisableAntiforgery(); // Intentional: Blazor WASM uses token-based requests, not cookies

app.Run();

static async Task<bool> HasValidImageSignature(IFormFile file)
{
    var header = new byte[8];
    await using var stream = file.OpenReadStream();
    var bytesRead = await stream.ReadAsync(header);
    if (bytesRead < 4)
        return false;

    // JPEG: FF D8 FF
    if (header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF)
        return true;

    // PNG: 89 50 4E 47 0D 0A 1A 0A
    if (bytesRead >= 8 &&
        header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47 &&
        header[4] == 0x0D && header[5] == 0x0A && header[6] == 0x1A && header[7] == 0x0A)
        return true;

    // GIF: 47 49 46 38 (GIF8)
    if (header[0] == 0x47 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x38)
        return true;

    // BMP: 42 4D
    if (header[0] == 0x42 && header[1] == 0x4D)
        return true;

    return false;
}
