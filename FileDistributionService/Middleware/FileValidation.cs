using Microsoft.EntityFrameworkCore;

namespace FileDistributionService.Middleware
{
    public class FileValidation
    {
        private readonly RequestDelegate _requestDelegate;
        private readonly IConfiguration _configuration;
        private readonly ILogger<FileValidation> _logger;

        public FileValidation(RequestDelegate requestDelegate, IConfiguration configuration, ILogger<FileValidation> logger)
        {
            _configuration = configuration;
            _requestDelegate = requestDelegate;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context, ApplicationContext _applicationContext)
        {
            // In GET request=>
            // a. Validate the download times of the file
            // b. Validate the file type/extension
            // c. Validate the file size
            if (context.Request.Method.ToUpper().Equals("GET"))
            {
                if (!ValidateDownloadTime())
                {
                    _logger.LogInformation("Invalidate download time");
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    return;
                }

                var fileId = Convert.ToString(context.Request.Query["id"]);
                if (fileId == null)
                {
                    _logger.LogInformation("Invalid file id in the HTTP request");
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    return;
                }

                var fileModel = await _applicationContext.Files.FirstOrDefaultAsync(o => o.Id.Equals(fileId));
                if (fileModel == null)
                {
                    _logger.LogInformation("File data doesn't not exist in the database");
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    return;
                }

                string filePath = Path.Combine(_configuration["FileSettings:FolderPath"], fileModel.Id);
                if (!File.Exists(filePath))
                {
                    _logger.LogInformation("File not found");
                    context.Response.StatusCode = StatusCodes.Status404NotFound;
                    return;
                }

                byte[] file = File.ReadAllBytes(filePath);
                var validateFileType = ValidateFileTypesForDownload(fileModel.Name);
                if (validateFileType)
                {
                    var validateFileSize = ValidateFileSize(file.Length);
                    if (!validateFileSize)
                    {
                        _logger.LogInformation($"Invalid file size of {file.Length}");
                        context.Response.StatusCode = StatusCodes.Status400BadRequest;
                        return;
                    }
                }

                context.Response.StatusCode = StatusCodes.Status200OK;
            }  
            // In POST request=>
            // a. Validate the file type/extension
            // b. Validate the file size
            else if (context.Request.Method.ToUpper().Equals("POST"))
            {
                var file = context.Request.Form.Files.FirstOrDefault();
                if (file == null)
                {
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    return;
                }

                var validateFileType = ValidateFileTypesForUpload(file.FileName);
                if (!validateFileType)
                {
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    return;
                }

                var validateFileSize = ValidateFileSize(file.Length);
                if (!validateFileSize)
                {
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    return;
                }
                context.Response.StatusCode = StatusCodes.Status200OK;
            }
            // To handle other methods like PUT, DELETE etc.
            else
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }

            await _requestDelegate(context);
        }

        ///check if the file name has extension that is valid or not
        public bool ValidateFileTypesForUpload(string fileName)
        {
            List<string> allowedExtensions = _configuration.GetSection("FileSettings:AllowedFileTypesForUpload").Get<List<string>>();
            if (!allowedExtensions.Contains(Path.GetExtension(fileName)))
            {
                return false;
            }

            return true;
        }

        ///check if the requested file id has extension that is valid or not
        public bool ValidateFileTypesForDownload(string fileId)
        {
            List<string> allowedExtensions = _configuration.GetSection("FileSettings:AllowedFileTypesForDownload").Get<List<string>>();
            if (!allowedExtensions.Any(o => fileId.Contains(o)))
            {
                return false;

            }

            return true;
        }

        // check if file size is less tha the max file size configured
        public bool ValidateFileSize(long fileSize)
        {
            long maxFileSize = (long)Convert.ToDouble(_configuration["FileSettings:MaxFileSizeInBytes"]);
            if (fileSize > maxFileSize)
            {
                return false;
            }

            return true;
        }

        // allow downloading of the file in between specific timings
        public bool ValidateDownloadTime()
        {
            var allowedDownloadStartTimeOfDay = TimeSpan.Parse(_configuration["FileSettings:AllowedDownloadStartTimeOfDay"]);
            var allowedDownloadEndTimeOfDay = TimeSpan.Parse(_configuration["FileSettings:AllowedDownloadEndTimeOfDay"]);
            var currentTimeOfDay = DateTime.Now.TimeOfDay;

            if (currentTimeOfDay < allowedDownloadStartTimeOfDay || currentTimeOfDay > allowedDownloadEndTimeOfDay)
            {
                return false;
            }

            return true;
        }

    }
}
