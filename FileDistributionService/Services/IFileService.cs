using System.Linq.Expressions;
using FileDistributionService;

namespace FileDistributionService
{
    public interface IFileService
    {
        Task<bool> UploadFile(IFormFile formFile);
        Task<FileStream?> DownloadFile(string fileId);
        Task<List<object>> GetAllFilesDetails(Expression<Func<FileModel, bool>> searchExpression);
    }
}
