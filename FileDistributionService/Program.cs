using FileDistributionService.Middleware;
using FileDistributionService;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Linq.Expressions;


var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile("appsettings.json");


// Configure structured logging
var logFilePath = builder.Configuration["Serilog:WriteTo:0:Args:path"];
var retainedFileCountLimit = Convert.ToInt16(builder.Configuration["Serilog:WriteTo:0:Args:retainedFileCountLimit"]);
var rollOnFileSizeLimit = Convert.ToBoolean(builder.Configuration["Serilog:WriteTo:0:Args:rollOnFileSizeLimit"]);

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .ReadFrom.Configuration(builder.Configuration)
    .WriteTo.File(logFilePath, rollingInterval: RollingInterval.Day, retainedFileCountLimit: 10, rollOnFileSizeLimit: rollOnFileSizeLimit)
    .CreateLogger();
builder.Host.UseSerilog();

//Configure database
builder.Services.AddDbContext<ApplicationContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
});


builder.Services.AddScoped<IFileService, FileService>();

var app = builder.Build();

app.UseMiddleware<ApiKeyAuthentication>();
app.UseSerilogRequestLogging();
app.UseMiddleware<GlobalException>();
app.UseMiddleware<FileValidation>();

app.MapGet("/files/dashboarddata", async (HttpContext context, IFileService fileService, ILogger<FileService> logger) =>
{
    DateTime? uploadedFromDate = null, uploadedToDate = null, lastDownloadedFromDate = null, lastDownloadedToDate = null;

    string fileName = context.Request.Query["fileName"];
    string uploadedFrom = context.Request.Query["uploadedFrom"];
    string uploadedTo = context.Request.Query["uploadedTo"];
    string lastDownloadedFrom = context.Request.Query["lastDownloadedFrom"];
    string lastDownloadedTo = context.Request.Query["lastDownloadedTo"];

    if (!string.IsNullOrEmpty(uploadedFrom) && DateTime.TryParse(uploadedFrom, out DateTime isUploadedFromDate))
    {
        uploadedFromDate = DateTime.SpecifyKind(Convert.ToDateTime(uploadedFrom), DateTimeKind.Utc);
    }
    if (!string.IsNullOrEmpty(uploadedTo) && DateTime.TryParse(uploadedTo, out DateTime isUploadedToDate))
    {
        uploadedToDate = DateTime.SpecifyKind(Convert.ToDateTime(uploadedTo), DateTimeKind.Utc);
    }
    if (!string.IsNullOrEmpty(lastDownloadedFrom) && DateTime.TryParse(lastDownloadedFrom, out DateTime isLastDownloadedFromDate))
    {
        lastDownloadedFromDate = DateTime.SpecifyKind(Convert.ToDateTime(lastDownloadedFrom), DateTimeKind.Utc);
    }
    if (!string.IsNullOrEmpty(lastDownloadedTo) && DateTime.TryParse(lastDownloadedTo, out DateTime isLastDownloadedToDate))
    {
        lastDownloadedToDate = DateTime.SpecifyKind(Convert.ToDateTime(lastDownloadedTo), DateTimeKind.Utc);
    }

    Expression<Func<FileModel, bool>> searchExpression = o =>
    (string.IsNullOrEmpty(fileName) || o.Name.Contains(fileName)) &&
    (!uploadedFromDate.HasValue || o.UploadedAt >= uploadedFromDate.Value) &&
    (!uploadedToDate.HasValue || o.UploadedAt <= uploadedToDate.Value) &&
    (!lastDownloadedFromDate.HasValue || o.LastDownloadedAt >= lastDownloadedFromDate.Value) &&
    (!lastDownloadedToDate.HasValue || o.LastDownloadedAt <= lastDownloadedToDate.Value);

    var allFileInfo = await fileService.GetAllFilesDetails(searchExpression);

    if (allFileInfo == null)
    {
        context.Response.StatusCode = StatusCodes.Status204NoContent;
    }

    context.Response.StatusCode = StatusCodes.Status200OK;
    context.Response.ContentType = "application/json";
    await context.Response.WriteAsJsonAsync(allFileInfo);

});

app.MapGet("/files/download", async (HttpContext context, string id, IFileService fileService, ILogger<FileService> logger) =>
{
    string filePath = Path.Combine(builder.Configuration["FileSettings:FolderPath"], id);

    var fileStream = await fileService.DownloadFile(id);
    if (fileStream != null)
    {
        context.Response.StatusCode = StatusCodes.Status200OK;
        logger.LogInformation($"Downloading {filePath}");

        await fileStream.CopyToAsync(context.Response.Body);
    }
    else
    {
        logger.LogInformation($"Downloading Failed {filePath}");
        context.Response.StatusCode = StatusCodes.Status404NotFound;
    }
});

// define route for uploading a file
app.MapPost("/files/upload", async (HttpContext context, IFileService fileService, ILogger<FileService> logger) =>
{
    var formData = await context.Request.ReadFormAsync();
    var file = formData.Files["file"];

    if (file != null)
    {
        var isDone = await fileService.UploadFile(file);
        context.Response.StatusCode = isDone ? StatusCodes.Status200OK : StatusCodes.Status500InternalServerError; ;
    }
    else
    {
        logger.LogInformation($"Upload file {file} is null");
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
    }
    await context.Response.WriteAsJsonAsync(formData);
});

app.Run();

