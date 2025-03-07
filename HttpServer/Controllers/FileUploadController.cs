using Microsoft.AspNetCore.Mvc;

using System.IO;
using System.Threading.Tasks;
using Backend.Models;
using HttpServer.asp.Dtos;
using System.Text.Json.Serialization;
using System.Text.Json;
using Shared.Dtos;

namespace Backend.Controllers;

[ApiController]
[Route("api/files")]
public class FileUploadController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IWebHostEnvironment _env;

    public FileUploadController(AppDbContext context, IWebHostEnvironment env)
    {
        _context = context;
        _env = env;
    }

    [HttpPost("uploadfile")]
    public async Task<IActionResult> UploadFile([FromForm] FileUploadDto uploadDto, [FromForm] string musicMetadataDto)
    {
        if (uploadDto.File == null || uploadDto.File.Length == 0)
        {
            return BadRequest("No file uploaded.");
        }

        var metadata = JsonSerializer.Deserialize<MusicMetadataDto>(musicMetadataDto);

        // Create a folder path to store the file
        var uploadsFolder = Path.Combine(_env.WebRootPath ?? _env.ContentRootPath, "uploads");
        if (!Directory.Exists(uploadsFolder))
        {
            Directory.CreateDirectory(uploadsFolder);
        }

        // Generate a unique file name (or use the original file name)
        var fileName = Path.GetFileName(uploadDto.File.FileName);
        var filePath = Path.Combine(uploadsFolder, fileName);

        // Save the file to the folder
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await uploadDto.File.CopyToAsync(stream);
        }

        // Create a FileReference instance for the database
        var fileReference = new FileReference
        {
            Name = fileName,
            FilePath = filePath // Or store a relative path if preferred
        };

        var MusicMetaData = new MusicMetadata()
        {
            Title = metadata.Title,
            Bpm = metadata.Bpm,
            Genre = metadata.Genre,
            UserId = metadata.UserId,
            FileReference = fileReference,
            
        };

        // Save the metadata to the database
        _context.Add(MusicMetaData);
        await _context.SaveChangesAsync();

        return Ok(new FileuploadResponseDto { message = "File uploaded successfully.", fileId = fileReference.Id });
    }
}

