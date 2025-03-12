

using Backend.Models;

namespace Backend.Models;


public class MusicMetadata
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;
    public int? Bpm { get; set; }
    public string? Genre { get; set; }
    //public bool IsVisible { get; set; }


    // Navigationals
    public int UserId { get; set; }
    public User User { get; set; }
    public int FileReferenceId { get; set; }
    public string FilePath { get; set; } = string.Empty;

}


