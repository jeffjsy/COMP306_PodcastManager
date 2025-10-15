using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using PodcastManagementSystem.Interfaces;
using System;
using System.IO;
using System.Threading.Tasks;

namespace PodcastManagementSystem.Services
{
    public class S3Service : IS3Service
    {
        private readonly IAmazonS3 _s3Client;
        private readonly string _bucketName;

        public S3Service(IAmazonS3 s3Client, IConfiguration configuration)
        {
            _s3Client = s3Client;
            // Retrieve the bucket name from appsettings.json
            _bucketName = configuration["AWS:S3BucketName"];
        }

        public async Task<string> UploadFileAsync(IFormFile file, int episodeId)
        {
            // 1. Define the unique key (e.g., episode-1/filename.mp3)
            var key = $"episodes/episode-{episodeId}/{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";

            var request = new PutObjectRequest
            {
                BucketName = _bucketName,
                Key = key,
                ContentType = file.ContentType
            };

            // 2. Stream the file content
            using (var stream = file.OpenReadStream())
            {
                request.InputStream = stream;
                await _s3Client.PutObjectAsync(request);
            }

            // 3. Return the public URL for storage in the Episodes table
            return $"https://{_bucketName}.s3.amazonaws.com/{key}";
        }

        public async Task<bool> DeleteFileAsync(string fileUrl)
        {
            // Logic to parse the URL and extract the key for deletion.
            var uri = new Uri(fileUrl);
            var key = uri.AbsolutePath.TrimStart('/');

            var request = new DeleteObjectRequest
            {
                BucketName = _bucketName,
                Key = key
            };

            await _s3Client.DeleteObjectAsync(request);
            return true;
        }
    }
}
