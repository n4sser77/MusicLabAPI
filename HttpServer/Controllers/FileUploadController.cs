using Backend.Models;
using HttpServer.asp.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using MusiclabApi;
using MusiclabApi.Services;
using Shared.Dtos;


namespace Backend.Controllers;

[ApiController]
[Route("api/files")]
public class FileUploadController : ControllerBase
{
    private readonly PostgresAppDb _context;
    private readonly IWebHostEnvironment _env;
    private readonly IWaveformGeneratorService _waveform;
    private readonly IStorageProvider _storage;
    // private readonly IMinioClient _minio;

    public FileUploadController(PostgresAppDb context, IWebHostEnvironment env, IWaveformGeneratorService waveGen, IStorageProvider storage)
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

        if (!file.ContentType.Contains("audio", StringComparison.InvariantCultureIgnoreCase))
        {
            return BadRequest(new FileuploadResponseDto { message = "File must be of type audio .",  });
        }

        // Get stream from form
        Stream filestram = file.OpenReadStream();
        // Upload to IStorageProvider
        var filePath = await _storage.UploadAsync(filestram, safeFileName, file.ContentType, metadata.UserId);

        System.Console.WriteLine("Filepath: " + filePath);
        //System.Console.WriteLine("Filepath: " + _storage.GetAbsolutePath(filePath));

        //var generateWaveformtask = _waveform.GenerateWaveformImage(_storage.GetAbsolutePath(filePath));
        var ms = await _storage.DownloadAsync(filePath, metadata.UserId);
        var bytes = await SupabaseBlob.ReadStreamToEndAsync(ms);
        var waveformTask = _waveform.GenerateWaveformImage(bytes);


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
            WaveFormImageBase64 = await waveformTask,

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

