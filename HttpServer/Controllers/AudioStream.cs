using Backend;
using Backend.asp.Services.Interfaces;
using HttpServer.asp.Services;
using HttpServer.asp.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Minio;
using Minio.DataModel;
using Minio.DataModel.Args;
using Shared.Dtos;
using System.Runtime.CompilerServices;
using System.Security.Claims;



namespace HttpServer.asp.Controllers
{
    [Route("api/audios")]
    public class AudioStream : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IJwtService _jwtService;
        private readonly IWebHostEnvironment _env;
        private readonly IWaveformGeneratorService _wave;
        private readonly IStorageProvider _storage;
        private readonly SignedUrlService _signedUrlService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        private string uploadsFolder;
        // private IMinioClient _minio;
        // private string bucketName = "music-files";

        public AudioStream(AppDbContext context, IWebHostEnvironment env, IJwtService jwtService, IWaveformGeneratorService wave, IStorageProvider storage, SignedUrlService signedUrlService, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _env = env;
            _jwtService = jwtService;
            _wave = wave;
            uploadsFolder = Path.Combine(_env.WebRootPath ?? _env.ContentRootPath, "uploads");
            _storage = storage;
            _signedUrlService = signedUrlService;
            _httpContextAccessor = httpContextAccessor;


        }

        [HttpGet("signed-url/{fileName}")]
        [Authorize]
        public async Task<IActionResult> GetSignedUrl(string fileName)
        {
            // Get the user ID from the authenticated token
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return Unauthorized();
            }

            // Check if the file exists and belongs to the user
            var file = await _context.MusicData.FirstOrDefaultAsync(m => m.FilePath == "user_" + userId + "/" + fileName);
            if (file == null)
            {
                return NotFound("File not found or not owned by user.");
            }

            // Use your storage provider to generate a pre-signed URL
            var request = _httpContextAccessor.HttpContext.Request;

            var signedUrl = _storage.GetSignedUrlAsync(fileName, file.UserId, TimeSpan.FromMinutes(60), $"{request.Scheme}://{request.Host}/api/audios"); // URL is valid for 10 minutes

            if (string.IsNullOrEmpty(signedUrl))
            {
                return BadRequest("Failed to generate signed URL.");
            }

            return Ok(signedUrl);
        }


        [HttpGet("{userId}/{fileName}")]
        [Authorize]
        public async Task<IActionResult> StreamAudio(int userId, string fileName, [FromQuery] long expires, [FromQuery] string sig)
        {
            // if (!_signedUrlService.ValidateSignature(userId, fileName, expires, sig))
            //     return Unauthorized();

            if (fileName == null) return NotFound("File not found");
            string filePath;
            if (!await _storage.ExistsAsync(fileName, userId))
            {
                return NotFound("File not found");
            }
            filePath = _storage.GetRelativeFilePath(userId, fileName);


            var file = await _context.MusicData.FirstOrDefaultAsync(m => m.FilePath == fileName);
            if (file is null) file = await _context.MusicData.FirstOrDefaultAsync(m => m.Title == Path.GetFileNameWithoutExtension(filePath));
            if (file is null) file = await _context.MusicData.FirstOrDefaultAsync(m => m.FilePath.Contains(filePath));




            if (string.IsNullOrEmpty(file.WaveFormImageBase64))
            {
                var waveform = _wave.GenerateWaveformImage(filePath);
                file.WaveFormImageBase64 = await waveform;
                await _context.SaveChangesAsync();
            }

            FileStream? fileStream = (FileStream?)await _storage.DownloadAsync(fileName, file.UserId);

            if (fileStream is null)
            {
                return NotFound("File not found");
            }

            return File(fileStream, "audio/mpeg", enableRangeProcessing: true);
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetAllAudio()
        {


            var userId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

            foreach (var claim in User.Claims)
            {
                Console.WriteLine($"Claim Type: {claim.Type}, Value: {claim.Value}");
            }


            int.TryParse(userId, out int userIdInt);


            var files = _context.MusicData.Where(m => m.UserId == userIdInt).ToList();
            var filesMetadataDto = new List<MusicMetadataDto>();
            if (files == null || files.Count == 0) return Ok(filesMetadataDto);

            foreach (var file in files)
            {
                var fRef = await _context.Files.FirstOrDefaultAsync(f => f.Id == file.FileReferenceId);
                if (fRef != null)
                    file.FilePath = Path.GetFileName(fRef.FilePath);



                var fileDto = new MusicMetadataDto
                {
                    Title = file.Title,
                    Bpm = file.Bpm,
                    FilePath = file.FilePath,
                    Id = file.Id,
                    Genre = file.Genre,
                    UserId = file.UserId,
                    WaveFormImageBase64 = file.WaveFormImageBase64
                };

                if (string.IsNullOrEmpty(fileDto.WaveFormImageBase64))
                {
                    var waveform = _wave.GenerateWaveformImage(fileDto.FilePath);
                    fileDto.WaveFormImageBase64 = await waveform;
                }
                filesMetadataDto.Add(fileDto);
            }





            return Ok(filesMetadataDto);
        }



        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteAudio(int id)
        {
            var userId = User.FindFirst("nameidentifier")?.Value;
            int.TryParse(userId, out int userIdInt);


            var file = await _context.MusicData.FirstOrDefaultAsync(m => m.Id == id);
            if (file == null) return NotFound("File not found");
            if (string.IsNullOrEmpty(file.FilePath))
            {
                var fileref = await _context.Files.FirstOrDefaultAsync(f => f.Id == file.FileReferenceId);
                file.FilePath = fileref.FilePath;
            }

            // System.IO.File.Delete(file.FilePath);
            bool isDeleted = await _storage.DeleteAsync(file.FilePath, file.UserId);
            if (!isDeleted) return NotFound("File not deleted or not found");
            _context.MusicData.Remove(file);


            await _context.SaveChangesAsync();
            return Ok("File is deleted");
        }



        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> UpdateAudio(int id, [FromBody] MusicMetadataDto updatedMetadataDto)
        {
            try
            {
                var userId = User.FindFirst("nameidentifier")?.Value;
                int.TryParse(userId, out int userIdInt);

                var fileToUpdate = await _context.MusicData.FirstOrDefaultAsync(m => m.Id == id);
                if (fileToUpdate == null) return NotFound("File not found");

                // Determine the Current File Path
                string currentFilePath = fileToUpdate.FilePath;
                if (string.IsNullOrEmpty(currentFilePath))
                {
                    var fileref = await _context.Files.FirstOrDefaultAsync(r => r.Id == fileToUpdate.FileReferenceId);
                    currentFilePath = fileref?.FilePath;
                }

                if (string.IsNullOrEmpty(currentFilePath))
                {
                    return BadRequest("File path not found in database.");
                }

                var oldFilenameWithExtension = Path.GetFileName(currentFilePath);
                var oldFilenameWithoutExtension = Path.GetFileNameWithoutExtension(oldFilenameWithExtension);
                var fileExtension = Path.GetExtension(oldFilenameWithExtension);

                string newFilePath = currentFilePath;

                // --- FIX: Clean the incoming title before use ---
                // 1. Ensure the new title does not contain an extension if the old one mistakenly did.
                string cleanedNewTitle = Path.GetFileNameWithoutExtension(updatedMetadataDto.Title);
                // The title in the DB should be the simple name without extension.
                fileToUpdate.Title = cleanedNewTitle;

                // Rename file if the title has changed
                if (oldFilenameWithoutExtension != cleanedNewTitle)
                {
                    // The new title is now clean, append the single, correct extension.
                    var newFilenameWithExtension = cleanedNewTitle + fileExtension;

                    var directoryPath = Path.GetDirectoryName(currentFilePath);
                    newFilePath = Path.Combine(directoryPath ?? string.Empty, newFilenameWithExtension);

                    bool isOK = await _storage.UpdateAsync(oldFilenameWithExtension, newFilenameWithExtension, fileToUpdate.UserId);
                    if (!isOK) return BadRequest(new { message = "Failed to rename file in storage." });
                }

                // Update Metadata in Database
                // fileToUpdate.Title has already been set to cleanedNewTitle above.
                fileToUpdate.Bpm = updatedMetadataDto.Bpm;
                fileToUpdate.Genre = updatedMetadataDto.Genre;
                fileToUpdate.FilePath = newFilePath;

                await _context.SaveChangesAsync();
            }
            catch (Exception)
            {
                return StatusCode(500, "An error occurred while processing the request.");
            }
            return Ok();
        }
    }
}