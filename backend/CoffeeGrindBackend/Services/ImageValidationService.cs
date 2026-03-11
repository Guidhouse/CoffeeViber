namespace CoffeeGrindBackend.Services;

public class ImageValidationService
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".gif", ".bmp"
    };

    private const long MaxFileSize = 10 * 1024 * 1024;

    public (bool Valid, string? Error) ValidateFile(IFormFile? file)
    {
        if (file is null || file.Length == 0)
            return (false, "No file uploaded.");

        if (file.Length > MaxFileSize)
            return (false, "File size exceeds the maximum limit of 10MB.");

        var extension = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(extension) || !AllowedExtensions.Contains(extension))
            return (false, "Only image files (JPG, PNG, GIF, BMP) are allowed.");

        return (true, null);
    }

    public async Task<bool> HasValidSignatureAsync(IFormFile file)
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
}
