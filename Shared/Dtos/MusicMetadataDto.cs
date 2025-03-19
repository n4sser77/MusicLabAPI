using System.ComponentModel;
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
        public string WaveFormImageBase64 { get; set; }
        /// <summary>
        /// implement later for when needing to hide from other users, 
        /// but know each user will only see their own audio.
        /// </summary>
        //public bool IsVisible { get; set; }

        // Navigational
        public int UserId { get; set; }
    }
}
