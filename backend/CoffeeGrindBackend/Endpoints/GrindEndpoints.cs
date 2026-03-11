using CoffeeGrindBackend.Services;

namespace CoffeeGrindBackend.Endpoints;

public static class GrindEndpoints
{
    public static WebApplication MapGrindEndpoints(this WebApplication app)
    {
        var grind = app.MapGroup("/api/grind");

        grind.MapPost("/upload", async (IFormFile? file, ImageValidationService validator) =>
        {
            var (valid, error) = validator.ValidateFile(file);
            if (!valid)
                return Results.BadRequest(error);

            if (!await validator.HasValidSignatureAsync(file!))
                return Results.BadRequest("File content does not match a supported image format.");

            try
            {
                // Store outside the web/content root so files are never directly URL-accessible
                var uploadsPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "CoffeeGrind", "Uploads");
                Directory.CreateDirectory(uploadsPath);

                var extension = Path.GetExtension(file!.FileName).ToLowerInvariant();
                var generatedFileName = $"{Guid.NewGuid()}{extension}";
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

        return app;
    }
}
