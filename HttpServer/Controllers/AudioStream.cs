using Backend;
using Backend.Migrations;
using Backend.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared.Dtos;

namespace HttpServer.asp.Controllers
{
    [Route("api/audio")]
    public class AudioStream : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;
        private string uploadsFolder;
        public AudioStream(AppDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
            uploadsFolder = Path.Combine(_env.WebRootPath ?? _env.ContentRootPath, "uploads");
        }

        [HttpGet("stream/{fileName}")]
        public async Task<IActionResult> StreamAudio(string fileName)
        {
            if (fileName == null) return NotFound("File not found");
            var filePath = Path.Combine(uploadsFolder, fileName);
            var file = _context.MusicData.FirstOrDefault(m => m.FilePath == filePath);
            if (!System.IO.File.Exists(filePath))
            {
                return NotFound("File not found.");
            }

            var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            return File(fileStream, "audio/mpeg", enableRangeProcessing: true);
        }

        [HttpGet("getall")]
        public async Task<IActionResult> GetAllAudio()
        {
            var files = _context.MusicData.ToList();
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
                    UserId = file.UserId

                };
                filesMetadataDto.Add(fileDto);
            }


            return Ok(filesMetadataDto);
        }

    }
}
