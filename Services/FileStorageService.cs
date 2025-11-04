using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using static System.Net.Mime.MediaTypeNames;

namespace Waster.Services
{
    public class FileStorageService : IFileStorageService
    {
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<FileStorageService> _logger;
        private readonly string _uploadFolder;

        public FileStorageService(IWebHostEnvironment environment, ILogger<FileStorageService> logger)
        {
            _environment = environment;
            _logger = logger;

            // Where to store images: wwwroot/uploads/images/
            _uploadFolder = Path.Combine(_environment.WebRootPath, "uploads", "images");

            // Create folder if doesn't exist
            if (!Directory.Exists(_uploadFolder))
            {
                Directory.CreateDirectory(_uploadFolder);
                _logger.LogInformation("Created upload folder: {Folder}", _uploadFolder);
            }
        }

        /// Save image to disk
        /// Example: Saves to wwwroot/uploads/images/abc123-def456.jpg
        /// Returns: /uploads/images/abc123-def456.jpg (what goes in database)
        public async Task<string> SaveImageAsync(byte[] imageData, string contentType)
        {
            try
            {
                // Generate unique filename
                var fileName = $"{Guid.NewGuid()}{GetFileExtension(contentType)}";

                // Full path on disk: C:/YourApp/wwwroot/uploads/images/abc123.jpg
                var physicalPath = Path.Combine(_uploadFolder, fileName);

                // Write bytes to file
                await File.WriteAllBytesAsync(physicalPath, imageData);

                // Return relative path (what we save in database)
                // This is what Flutter dev will use
                var relativePath = $"/uploads/images/{fileName}";

                _logger.LogInformation("Saved image: {Path} ({Size} bytes)", relativePath, imageData.Length);

                return relativePath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save image");
                throw;
            }
        }

        public bool DeleteImageAsync(string imagePath)
        {
            try
            {
                // Convert relative path to physical path
                // /uploads/images/abc123.jpg → C:/YourApp/wwwroot/uploads/images/abc123.jpg
                var physicalPath = GetPhysicalPath(imagePath);

                if (File.Exists(physicalPath))
                {
                    // Delete from disk
                     File.Delete(physicalPath);
                    _logger.LogInformation("Deleted image: {Path}", imagePath);
                    return true;
                }

                _logger.LogWarning("Image not found for deletion: {Path}", imagePath);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete image: {Path}", imagePath);
                return false;
            }
        }

        public string GetPhysicalPath(string imagePath)
        {
            // Remove leading slash if present
            var cleanPath = imagePath.TrimStart('/');

            // Combine with web root
            return Path.Combine(_environment.WebRootPath, cleanPath);
        }

        /// Get file extension from content type
        private static string GetFileExtension(string contentType)
        {
            return contentType?.ToLower() switch
            {
                "image/jpeg" => ".jpg",
                "image/jpg" => ".jpg",
                "image/png" => ".png",
                "image/gif" => ".gif",
                "image/webp" => ".webp",
                _ => ".jpg"
            };
        }
    }
}
