using Amazon.S3;
using Amazon.S3.Model;

namespace PryectoDeGraduacionBACK.Services.S3;

public class S3Service : IS3Service
{
    private readonly IAmazonS3 _s3;
    private readonly IConfiguration _configuration;

    public S3Service(
        IAmazonS3 s3,
        IConfiguration configuration)
    {
        _s3 = s3;
        _configuration = configuration;
    }

    public async Task<string> UploadAsync(
        Stream file,
        string fileName,
        string contentType,
        string folder)
    {
        var bucketName = _configuration["AWS:BucketName"];

        var key = $"{folder}/{Guid.NewGuid()}-{fileName}";

        var request = new PutObjectRequest
        {
            BucketName = bucketName,
            Key = key,
            InputStream = file,
            ContentType = contentType
        };

        await _s3.PutObjectAsync(request);

        return key;
    }

    public async Task DeleteAsync(string key)
    {
        var bucketName = _configuration["AWS:BucketName"];

        var request = new DeleteObjectRequest
        {
            BucketName = bucketName,
            Key = key
        };

        await _s3.DeleteObjectAsync(request);
    }
}