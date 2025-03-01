

using Backend.Models;

namespace Backend.Models;


public class MusicMetadata
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;
    public int BPM { get; set; }
    public string? Genre { get; set; }


    // Navigationals
    public int FileReferenceId { get; set; }
    public FileReference FileReference { get; set; } = new FileReference();

}


