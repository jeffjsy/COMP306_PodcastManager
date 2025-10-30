namespace PodcastManagementSystem.Interfaces
{
    public interface IS3Service
    {

        Task<string> UploadFileAsync(IFormFile file, string fileKey);

        // Deletes the file from S3 using its URL or key.
        Task<bool> DeleteFileAsync(string fileUrl);
    }
}
