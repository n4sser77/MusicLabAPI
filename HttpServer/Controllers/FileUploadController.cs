using Microsoft.AspNetCore.Mvc;

using System.IO;
using System.Threading.Tasks;
using Backend.Models;
using HttpServer.asp.Dtos;
using System.Text.Json.Serialization;
using System.Text.Json;
using Shared.Dtos;
using HttpServer.asp.Services.Interfaces;
using Minio;
using Minio.DataModel.Args;
using Minio.DataModel;
using Minio.DataModel.Encryption;
using NAudio.MediaFoundation;

namespace Backend.Controllers;

[ApiController]
[Route("api/files")]
public class FileUploadController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IWebHostEnvironment _env;
    private readonly IWaveformGeneratorService _waveform;
    private readonly IStorageProvider _storage;
    // private readonly IMinioClient _minio;

    public FileUploadController(AppDbContext context, IWebHostEnvironment env, IWaveformGeneratorService waveGen, IStorageProvider storage)
    {
        _context = context;
        _env = env;
        _waveform = waveGen;
        // _minio = minioclient;
        _storage = storage;
    }

    [HttpPost("uploadfile")]
    public async Task<IActionResult> UploadFile([FromForm] IFormFile file, [FromForm] MusicMetadataDto musicMetadataDto)
    {

        if (file == null || file.Length == 0)
        {
            return BadRequest("No file uploaded.");
        }

        // var metadata = JsonSerializer.Deserialize<MusicMetadataDto>(musicMetadataDto);
        var metadata = musicMetadataDto;
        if (metadata is null)
        {
            return BadRequest("Meta data is missing");
        }



        // // Create a folder path to store the file -- old way
        // var UPLOADS_DIR = Path.Combine(_env.WebRootPath ?? _env.ContentRootPath, "uploads");
        // if (!Directory.Exists(UPLOADS_DIR))
        // {
        //     Directory.CreateDirectory(UPLOADS_DIR);
        // }

        // // Generate a unique file name (or use the original file name)
        // var filePath = Path.Combine(UPLOADS_DIR, fileName);



        var safeFileName = Path.GetFileName(file.FileName.Replace(" ", "_"));

        if (file.ContentType != "audio")
        {

        }

        // Get stream from form
        Stream filestram = file.OpenReadStream();
        // Upload to IStorageProvider
        var filePath = await _storage.UploadAsync(filestram, safeFileName, file.ContentType, metadata.UserId);

        System.Console.WriteLine("Filepath: " + filePath);
        System.Console.WriteLine("Filepath: " + _storage.GetAbsolutePath(filePath));

        var generateWaveformtask = _waveform.GenerateWaveformImage(_storage.GetAbsolutePath(filePath));

        // Create a FileReference instance for the database
        var fileReference = new FileReference
        {
            Name = safeFileName,
            FilePath = filePath
        };

        var musicMetaData = new MusicMetadata()
        {
            Title = metadata.Title,
            Bpm = metadata.Bpm,
            Genre = metadata.Genre,
            UserId = metadata.UserId,
            FilePath = filePath,
            WaveFormImageBase64 = await generateWaveformtask,

        };

        // Save the metadata to the database
        _context.Add(musicMetaData);
        await _context.SaveChangesAsync();


        // Save the file to the folder -- old way
        // using (var stream = new FileStream(filePath, FileMode.Create))
        // {
        //     await uploadDto.File.CopyToAsync(stream);

        // }


        return Ok(new FileuploadResponseDto { message = "File uploaded successfully.", fileId = fileReference.Id });
    }
}

