namespace FileDistributionService
{
    public interface IFileService
    {
        Task<bool> UploadFile(IFormFile formFile);
        Task<FileStream?> DownloadFile(string fileId);
    }
}
