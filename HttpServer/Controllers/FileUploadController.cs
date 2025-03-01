using Microsoft.AspNetCore.Mvc;

using System.IO;
using System.Threading.Tasks;
using Backend;
using Backend.Models;

namespace Backend.Controllers;

    [ApiController]
    [Route("api/[controller]")]
    public class FileUploadController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;

        public FileUploadController(AppDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        [HttpPost]
        public async Task<IActionResult> UploadFile([FromForm] FileUploadDto uploadDto)
        {
            if (uploadDto.File == null || uploadDto.File.Length == 0)
            {
                return BadRequest("No file uploaded.");
            }

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

            // Save the metadata to the database
            _context.Files.Add(fileReference);
            await _context.SaveChangesAsync();

            return Ok(new { message = "File uploaded successfully.", fileId = fileReference.Id });
        }
    }

