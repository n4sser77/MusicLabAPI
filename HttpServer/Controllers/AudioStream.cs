using Backend;
using Backend.asp.Services.Interfaces;
using HttpServer.asp.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
        private string uploadsFolder;
        public AudioStream(AppDbContext context, IWebHostEnvironment env, IJwtService jwtService, IWaveformGeneratorService wave)
        {
            _context = context;
            _env = env;
            _jwtService = jwtService;
            _wave = wave;
            uploadsFolder = Path.Combine(_env.WebRootPath ?? _env.ContentRootPath, "uploads");
        }

        [HttpGet("stream/{fileName}")]
        public async Task<IActionResult> StreamAudio(string fileName)
        {
            if (fileName == null) return NotFound("File not found");
            var filePath = Path.Combine(uploadsFolder, fileName);
            var file = await _context.MusicData.FirstOrDefaultAsync(m => m.FilePath == fileName);
            if (file == null) file = await _context.MusicData.FirstOrDefaultAsync(m => m.Title == Path.GetFileNameWithoutExtension(filePath));
            if (!System.IO.File.Exists(filePath))
            {
                return NotFound("File not found.");
            }
            if (string.IsNullOrEmpty(file.WaveFormImageBase64))
            {
                var waveform = _wave.GenerateWaveformImage(filePath);
                file.WaveFormImageBase64 = await waveform;
                await _context.SaveChangesAsync();
            }
            var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            return File(fileStream, "audio/mpeg", enableRangeProcessing: true);
        }

        [HttpGet("getall")]
        public async Task<IActionResult> GetAllAudio()
        {

            var result = TryGetUserIdFromToken(out int userIdInt);
            if (result != null) return result;

            var files = _context.MusicData.Where(m => m.UserId == userIdInt).ToList();
            var filesMetadataDto = new List<MusicMetadataDto>();
            if (files == null || files.Count == 0) return BadRequest("No files found");

            foreach (var file in files)
            {
                var fRef = await _context.Files.FirstOrDefaultAsync(f => f.Id == file.FileReferenceId);
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
        public async Task<IActionResult> DeleteAudio(int id)
        {
            var result = TryGetUserIdFromToken(out int userIdInt);
            if (result != null) return result;

            var file = await _context.MusicData.FirstOrDefaultAsync(m => m.Id == id);
            if (file == null) return NotFound("File not found");
            if (string.IsNullOrEmpty(file.FilePath))
            {
                var fileref = await _context.Files.FirstOrDefaultAsync(f => f.Id == file.FileReferenceId);
                file.FilePath = fileref.FilePath;
            }

            System.IO.File.Delete(file.FilePath);
            _context.MusicData.Remove(file);
            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpPut("update/{id}")]
        public async Task<IActionResult> UpdateAudio(int id, [FromBody] MusicMetadataDto updatedMetadataDto)
        {
            try
            {
                var result = TryGetUserIdFromToken(out int userIdInt);
                if (result != null) return result;

                var fileToUpdate = await _context.MusicData.FirstOrDefaultAsync(m => m.Id == id);
                if (fileToUpdate == null) return NotFound("File not found");

                if (string.IsNullOrEmpty(fileToUpdate.FilePath))
                {
                    var fileref = _context.Files.FirstOrDefault(r => r.Id == fileToUpdate.FileReferenceId);
                    System.IO.File.Move(Path.Combine(uploadsFolder, fileref.FilePath), Path.Combine(uploadsFolder, updatedMetadataDto.FilePath));

                }
                else
                {

                    System.IO.File.Move(Path.Combine(uploadsFolder, fileToUpdate.FilePath), Path.Combine(uploadsFolder, updatedMetadataDto.FilePath));
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
