using Backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace DailyOneRosterFile.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FilesController(IOptions<StorageOptions> storageOptions) : ControllerBase
{
    private readonly string _storagePath = storageOptions.Value.GeneratedFilesPath;

    [HttpGet("latest-oneroster")]
    public IActionResult DownloadLatestFile()
    {
        var files = Directory.GetFiles(_storagePath, "*.zip");
        if (files.Length == 0)
        {
            return NotFound("No files generated yet.");
        }

        // Get the most recent file
        var latestFile = files
            .OrderByDescending(path => System.IO.File.GetLastWriteTimeUtc(path))
            .First();

        var bytes = System.IO.File.ReadAllBytes(latestFile);
        return File(bytes, "application/zip", Path.GetFileName(latestFile));
    }
}