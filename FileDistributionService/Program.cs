using FileDistributionService;
using FileDistributionService.Middleware;
using Microsoft.EntityFrameworkCore;
using Serilog;

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

app.UseSerilogRequestLogging();

app.UseMiddleware<FileValidation>();
app.UseMiddleware<GlobalException>();

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