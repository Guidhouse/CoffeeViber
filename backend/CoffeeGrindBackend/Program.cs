var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
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

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors("Frontend");

app.MapPost("/api/grind/upload", async (IFormFile? file, IWebHostEnvironment environment) =>
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

    try
    {
        var uploadsPath = Path.Combine(environment.ContentRootPath, "Uploads");
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
}).DisableAntiforgery();

app.Run();
