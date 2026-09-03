namespace PryectoDeGraduacionBACK.Services.S3;

public interface IS3Service
{
    Task<string> UploadAsync(
        Stream file,
        string fileName,
        string contentType,
        string folder);

    string GetPublicUrl(string key);

    Task DeleteAsync(string key);

    Task DeleteByUrlAsync(string fileUrl);
}