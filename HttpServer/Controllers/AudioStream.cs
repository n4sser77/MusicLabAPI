using Backend;
using Backend.asp.Services.Interfaces;
using Backend.Migrations;
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
    [Route("api/audio")]
    public class AudioStream : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IJwtService _jwtService;
        private readonly IWebHostEnvironment _env;
        private readonly IWaveformGeneratorService _wave;
        private readonly IStorageProvider _storage;
        private string uploadsFolder;
        // private IMinioClient _minio;
        // private string bucketName = "music-files";

        public AudioStream(AppDbContext context, IWebHostEnvironment env, IJwtService jwtService, IWaveformGeneratorService wave, IStorageProvider storage)
        {
            _context = context;
            _env = env;
            _jwtService = jwtService;
            _wave = wave;
            uploadsFolder = Path.Combine(_env.WebRootPath ?? _env.ContentRootPath, "uploads");
            _storage = storage;

        }

        [HttpGet("stream/{userId}/{fileName}/")]

        public async Task<IActionResult> StreamAudio(string fileName, int userId)
        {
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

        [HttpGet("getall")]
        [Authorize]
        public async Task<IActionResult> GetAllAudio()
        {

            // var result = TryGetUserIdFromToken(out int userIdInt);
            // if (result != null) return result;
            //var userId = User.FindFirstValue("sub");
            var userId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

            foreach (var claim in User.Claims)
            {
                Console.WriteLine($"Claim Type: {claim.Type}, Value: {claim.Value}");
            }


            int.TryParse(userId, out int userIdInt);


            var files = _context.MusicData.Where(m => m.UserId == userIdInt).ToList();
            var filesMetadataDto = new List<MusicMetadataDto>();
            if (files == null || files.Count == 0) return BadRequest("No files found");

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


        // manually validate token
        private IActionResult TryGetUserIdFromToken(out int userId)
        {
            userId = 0;
            var authHeader = Request.Headers.Authorization.FirstOrDefault();

            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
            {
                return Unauthorized();
            }

            var token = authHeader.Substring("Bearer ".Length);
            var claimDictionary = _jwtService.ValidateToken(token);

            if (claimDictionary == null || !claimDictionary.TryGetValue(ClaimTypes.NameIdentifier, out var userIdStr))
            {
                return Unauthorized();
            }

            if (!int.TryParse(userIdStr, out userId))
            {
                return Unauthorized();
            }

            return null; // Indicating success
        }




        [HttpDelete("delete/{id}")]
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

        [HttpPut("update/{id}")]
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
