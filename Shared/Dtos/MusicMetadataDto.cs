namespace Shared.Dtos
{
    public class MusicMetadataDto
    {
        public string Title { get; set; }
        public int Bpm { get; set; }
        public string Genre { get; set; }

        // Navigational
        public int UserId { get; set; }
    }
}
