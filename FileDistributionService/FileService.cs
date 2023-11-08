using Serilog;

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

            string filePath =  Path.Combine(_configuration["FileSettings:FolderPath"], fileId);
            if (File.Exists(filePath))
            {
                return new FileStream(filePath, FileMode.Open);
            }

            _logger.LogInformation($"{fileId} does not exists.");
            return null;
            ;
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
                Path = filePath
            };

            _applicationContext.Files.Add(fileModel);
            await _applicationContext.SaveChangesAsync();

            return true;

        }
    }
}
