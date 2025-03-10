using System.Text.Json;

namespace Shared.Dtos
{
    public class MusicMetadataDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public int? Bpm { get; set; }
        public string? Genre { get; set; }
        public string FilePath { get; set; }

        // Navigational
        public int UserId { get; set; }
    }
}
