namespace WebsiteBanGiay.Services
{
    public interface IFileService
    {
        Task<string> UploadFileAsync(IFormFile file, string subFolder);
        Task DeleteFileAsync(string filePath);
    }
}
