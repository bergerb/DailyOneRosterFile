using Microsoft.AspNetCore.Mvc;

namespace DailyOneRosterFile.Api.Models;

public class UploadFileDto
{
    [FromForm]
    public IFormFile? File { get; set; }
}
