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
        private string uploadsFolder;
        // private IMinioClient _minio;
        // private string bucketName = "music-files";

        public AudioStream(AppDbContext context, IWebHostEnvironment env, IJwtService jwtService, IWaveformGeneratorService wave, IStorageProvider storage, SignedUrlService signedUrlService)
        {
            _context = context;
            _env = env;
            _jwtService = jwtService;
            _wave = wave;
            uploadsFolder = Path.Combine(_env.WebRootPath ?? _env.ContentRootPath, "uploads");
            _storage = storage;
            _signedUrlService = signedUrlService;

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
            // This method needs to be implemented in your IStorageProvider
            var signedUrl = _storage.GetSignedUrlAsync(fileName, file.UserId, TimeSpan.FromMinutes(60), "http://localhost:5106/api/audios"); // URL is valid for 10 minutes

            if (string.IsNullOrEmpty(signedUrl))
            {
                return BadRequest("Failed to generate signed URL.");
            }

            return Ok(signedUrl);
        }


        [HttpGet("{userId}/{fileName}/")]
        public async Task<IActionResult> StreamAudio(int userId, string fileName, [FromQuery] long expires, [FromQuery] string sig)
        {
            if (!_signedUrlService.ValidateSignature(userId, fileName, expires, sig))
                return Unauthorized();

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

                if (string.IsNullOrEmpty(fileToUpdate.FilePath))
                {
                    var fileref = _context.Files.FirstOrDefault(r => r.Id == fileToUpdate.FileReferenceId);
                    // System.IO.File.Move(Path.Combine(uploadsFolder, fileref.FilePath), Path.Combine(uploadsFolder, updatedMetadataDto.FilePath));
                    var oldFilename = Path.GetFileName(fileref?.FilePath);
                    var newFilename = Path.GetFileName(updatedMetadataDto.FilePath);
                    if (string.IsNullOrEmpty(oldFilename))
                    {
                        return BadRequest("File not present");
                    }


                    bool isOK = await _storage.UpdateAsync(oldFilename, newFilename, fileToUpdate.UserId);
                    if (!isOK) return BadRequest(new { message = "Failed to save file" });

                }
                else
                {

                    // System.IO.File.Move(Path.Combine(uploadsFolder, fileToUpdate.FilePath), Path.Combine(uploadsFolder, updatedMetadataDto.FilePath));
                    var oldFilename = Path.GetFileName(fileToUpdate.FilePath);
                    var newFilename = Path.GetFileName(updatedMetadataDto.FilePath);

                    bool isOK = await _storage.UpdateAsync(oldFilename, newFilename, fileToUpdate.UserId);
                    if (!isOK) return BadRequest(new { message = "Failed to save file" });
                }

                fileToUpdate.Title = updatedMetadataDto.Title;
                fileToUpdate.Bpm = updatedMetadataDto.Bpm;
                fileToUpdate.Genre = updatedMetadataDto.Genre;
                fileToUpdate.FilePath = updatedMetadataDto.FilePath;

                await _context.SaveChangesAsync();

            }
            catch (Exception)
            {

                return BadRequest();
            }
            return Ok();
        }

    }
}
