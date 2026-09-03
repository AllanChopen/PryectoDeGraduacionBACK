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
        var bucketName = GetRequiredConfiguration("AWS:BucketName");

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

    public string GetPublicUrl(string key)
    {
        var bucketName = GetRequiredConfiguration("AWS:BucketName");
        var region = GetRequiredConfiguration("AWS:Region");
        var host = region.Equals("us-east-1", StringComparison.OrdinalIgnoreCase)
            ? $"{bucketName}.s3.amazonaws.com"
            : $"{bucketName}.s3.{region}.amazonaws.com";

        return new UriBuilder(Uri.UriSchemeHttps, host)
        {
            Path = key
        }.Uri.AbsoluteUri;
    }

    public async Task DeleteAsync(string key)
    {
        var bucketName = GetRequiredConfiguration("AWS:BucketName");

        var request = new DeleteObjectRequest
        {
            BucketName = bucketName,
            Key = key
        };

        await _s3.DeleteObjectAsync(request);
    }

    public async Task DeleteByUrlAsync(string fileUrl)
    {
        if (!Uri.TryCreate(fileUrl, UriKind.Absolute, out var uri))
        {
            throw new ArgumentException("La URL del archivo de S3 no es válida.", nameof(fileUrl));
        }

        var expectedHost = new Uri(GetPublicUrl("archivo")).Host;
        if (!uri.Host.Equals(expectedHost, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("La URL no pertenece al bucket S3 configurado.", nameof(fileUrl));
        }

        var key = Uri.UnescapeDataString(uri.AbsolutePath.TrimStart('/'));
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("La URL no contiene una key de S3.", nameof(fileUrl));
        }

        await DeleteAsync(key);
    }

    private string GetRequiredConfiguration(string key)
    {
        return _configuration[key]
            ?? throw new InvalidOperationException($"No se configuró '{key}'.");
    }
}