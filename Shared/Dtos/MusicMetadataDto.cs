using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Shared.Dtos
{
    public class MusicMetadataDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; }
        [JsonPropertyName("bpm")]
        public int? Bpm { get; set; }
        [JsonPropertyName("genre")]
        public string? Genre { get; set; }
        [JsonPropertyName("filepath")]
        public string? FilePath { get; set; }
        [JsonPropertyName("waveformImageBase64")]
        public string? WaveFormImageBase64 { get; set; }
        /// <summary>
        /// implement later for when needing to hide from other users, 
        /// but know each user will only see their own audio.
        /// </summary>
        //public bool IsVisible { get; set; }

        // Navigational
        [JsonPropertyName("userId")]
        public int UserId { get; set; }
    }
}
