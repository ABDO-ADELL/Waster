namespace Waster.Services
{
    public interface IFileStorageService
    {
        /// Save image to server's file system
        Task<string> SaveImageAsync(byte[] imageData, string contentType);

        /// Delete image from file system
        bool DeleteImageAsync(string imagePath);

        /// Get full physical path to image
        string GetPhysicalPath(string imagePath);
    }
}