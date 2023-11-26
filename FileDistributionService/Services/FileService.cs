using FileDistributionService.Middleware;
using FileDistributionService;
using FileDistributionService;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Linq.Expressions;

namespace FileDistributionService
{
    public class FileService : IFileService
    {
        private readonly ApplicationContext _applicationContext;
        private readonly IConfiguration _configuration;
        private readonly ILogger<FileService> _logger;

        public FileService(ApplicationContext applicationContext, IConfiguration configuration, ILogger<FileService> logger)
        {
            _configuration = configuration;
            _applicationContext = applicationContext;
            _logger = logger;
        }

        public async Task<FileStream?> DownloadFile(string fileId)
        {

            string filePath = Path.Combine(_configuration["FileSettings:FolderPath"], fileId);
            if (File.Exists(filePath))
            {
                try
                {
                    var file = _applicationContext.Files.FirstOrDefault(o => o.Id.Equals(fileId));
                    file.LastDownloadedAt = DateTime.UtcNow;
                    file.TotalDownloads++;

                    // Save changes to the database
                    await _applicationContext.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    throw new DatabaseException();
                }
                return new FileStream(filePath, FileMode.Open);
            }

            _logger.LogInformation($"{fileId} does not exists.");

            return null;
        }

        public async Task<bool> UploadFile(IFormFile formFile)
        {
            //This guid + filename combination is the primary key
            string fileId = Guid.NewGuid().ToString() + "_" + formFile.FileName;
            string filePath = Path.Combine(_configuration["FileSettings:FolderPath"], fileId);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await formFile.CopyToAsync(fileStream);
            }

            FileModel fileModel = new FileModel
            {
                Id = fileId,
                Name = formFile.FileName,
                Path = filePath,
                Size = formFile.Length,
                Type = formFile.ContentType,
                UploadedAt = DateTime.UtcNow,
            };
            try
            {
                _applicationContext.Files.Add(fileModel);
                await _applicationContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                throw new DatabaseException();
            }

            return true;
        }

        public Task<List<object>> GetAllFilesDetails(Expression<Func<FileModel, bool>> searchExpression)
        {
            return _applicationContext.Files.Where(searchExpression).Select(o => new
            {
                FileId = o.Id,
                FileName = o.Name,
                FilePath = o.Path,
                FileSizeInMB = Math.Round((double)o.Size / (1024 * 1024), 2),
                FileType = o.Type,
                o.TotalDownloads,
                UploadedAt = o.UploadedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                LastDownloadedAt = o.LastDownloadedAt != null? Convert.ToDateTime(o.LastDownloadedAt).ToString("yyyy-MM-dd HH:mm:ss"): ""
            }).ToListAsync<object>();
        }
    }
}
