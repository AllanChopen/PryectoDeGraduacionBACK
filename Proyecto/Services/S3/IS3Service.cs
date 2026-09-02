namespace PryectoDeGraduacionBACK.Services.S3;

public interface IS3Service
{
    Task<string> UploadAsync(
        Stream file,
        string fileName,
        string contentType,
        string folder);

    Task DeleteAsync(string key);
}