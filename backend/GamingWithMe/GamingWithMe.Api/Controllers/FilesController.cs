using Amazon.S3;
using Amazon.S3.Model;
using GamingWithMe.Application.Dtos;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GamingWithMe.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FilesController : ControllerBase
    {
        private readonly IAmazonS3 _s3Client;
        private readonly string bucketname = "gamingwithme";

        public FilesController(IAmazonS3 s3Client)
        {
            _s3Client = s3Client;
        }

        [HttpPost]
        public async Task<IActionResult> UploadFileAsync(IFormFile file, string? prefix)
        {
            var request = new PutObjectRequest()
            {
                BucketName = bucketname,
                Key = string.IsNullOrEmpty(prefix) ? file.FileName : $"{prefix?.TrimEnd('/')}/{file.FileName}",
                InputStream = file.OpenReadStream()
            };

            request.Metadata.Add("Content-Type", file.ContentType);
            await _s3Client.PutObjectAsync(request);
            return Ok($"File {prefix}/{file.FileName} uploaded to s3 yesss");
        }

        [HttpGet]
        public async Task<IActionResult> GetAllFilesAsync(string? prefix)
        {
            var request = new ListObjectsV2Request()
            {
                BucketName = bucketname,
                Prefix = prefix
            };
            var result = await _s3Client.ListObjectsV2Async(request);
            var s3Objects = result.S3Objects.Select(x =>
            {
                var urlRequest = new GetPreSignedUrlRequest()
                {
                    BucketName = bucketname,
                    Key = x.Key,
                    Expires = DateTime.UtcNow.AddMinutes(1)
                };

                return new S3ObjectDto(x.Key.ToString(), _s3Client.GetPreSignedURL(urlRequest));
            });

            return Ok(s3Objects);
        }

        [HttpGet("preview")]
        public async Task<IActionResult> PreviewFileAsync([FromQuery] string key)
        {
            if (string.IsNullOrEmpty(key))
                return BadRequest("Key is required");

            var request = new GetObjectRequest
            {
                BucketName = bucketname,
                Key = key
            };

            try
            {
                using var response = await _s3Client.GetObjectAsync(request);
                return File(response.ResponseStream, response.Headers.ContentType ?? "application/octet-stream", response.Key);
            }
            catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return NotFound();
            }
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteFileAsync([FromQuery] string key)
        {
            if (string.IsNullOrEmpty(key))
                return BadRequest("Key is required");

            var request = new DeleteObjectRequest
            {
                BucketName = bucketname,
                Key = key
            };

            await _s3Client.DeleteObjectAsync(request);
            return Ok($"File {key} deleted from S3");
        }

    }
}
