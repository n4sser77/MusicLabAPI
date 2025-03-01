using Microsoft.AspNetCore.Http;
namespace Backend;


public class FileUploadDto
{
    // This property will be automatically populated by ASP.NET Core when a file is uploaded.
    public IFormFile File { get; set; }
}

