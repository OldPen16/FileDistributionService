using FileDistributionService;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

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
            var path = context.Request.Path;
            if (path.StartsWithSegments("/files/upload"))
            {
                var file = context.Request.Form.Files.FirstOrDefault();
                if (file == null)
                {
                    context = SetHttpContextResponse(context, "File is null.");
                    return;
                }

                var validateFileType = ValidateFileTypesForUpload(file.FileName);
                if (!validateFileType)
                {
                    context = SetHttpContextResponse(context, "Invalidate file type.");
                    return;
                }

                var validateFileSize = ValidateFileSize(file.Length);
                if (!validateFileSize)
                {
                    context = SetHttpContextResponse(context, "Invalidate file size.");
                    return;
                }
                context.Response.StatusCode = StatusCodes.Status200OK;
            }
            else if (path.StartsWithSegments("/files/download"))
            {
                try
                {
                    if (!ValidateDownloadTime())
                    {
                        context = SetHttpContextResponse(context, "Invalidate download time.");
                        return;
                    }

                    var fileId = context.Request.Query["id"];
                    if (String.IsNullOrEmpty(fileId))
                    {
                        context = SetHttpContextResponse(context, "Invalid parameter passed.");
                        return;
                    }

                    var fileModel = await _applicationContext.Files.FirstOrDefaultAsync(o => o.Id.Equals(fileId));
                    if (fileModel == null)
                    {
                        context = SetHttpContextResponse(context, "File data does not exist in the database.");
                        return;
                    }

                    if (!File.Exists(fileModel.Path))
                    {
                        context = SetHttpContextResponse(context, "File not found");
                        return;
                    }

                    byte[] file = File.ReadAllBytes(fileModel.Path);
                    var validateFileType = ValidateFileTypesForDownload(fileModel.Name);
                    if (validateFileType)
                    {
                        var validateFileSize = ValidateFileSize(file.Length);
                        if (!validateFileSize)
                        {
                            context = SetHttpContextResponse(context, "Invalid file size.");
                            return;
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw new FileException();
                }
                context.Response.StatusCode = StatusCodes.Status200OK;
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

        public HttpContext SetHttpContextResponse(HttpContext context, string errorMessage)
        {
            _logger.LogError(errorMessage);
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            context.Response.WriteAsync(errorMessage);

            return context;
        }
    }
}
