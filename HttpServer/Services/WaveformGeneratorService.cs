using HttpServer.asp.Services.Interfaces;
using NAudio.Wave;
using NAudio.WaveFormRenderer;

namespace HttpServer.asp.Services;

public class WaveformGeneratorService : IWaveformGeneratorService
{
    private readonly IWebHostEnvironment _env;
    private readonly string uploadsFolder;

    /// <summary>
    /// Generate waveform image from audio file
    /// </summary>
    /// <param name="filepath"></param>
    /// <returns>returns base64 string</returns>

    public WaveformGeneratorService(IWebHostEnvironment env)
    {
        _env = env;
        uploadsFolder = Path.Combine(_env.WebRootPath ?? _env.ContentRootPath, "uploads");
    }

    public async Task<string> GenerateWaveformImage(string filepath)
    {
        string imageBase64 = string.Empty;

        //  waveform settings for the renderer
        var myRendererSettings = new StandardWaveFormRendererSettings();
        myRendererSettings.Width = 710;
        myRendererSettings.TopHeight = 32;
        myRendererSettings.BottomHeight = 32;
        myRendererSettings.BackgroundColor = System.Drawing.Color.Transparent;


        var maxPeakProvider = new MaxPeakProvider();
        var renderer = new WaveFormRenderer();
        using var audioStream = new AudioFileReader(Path.Combine(uploadsFolder,filepath));
        var image = renderer.Render(audioStream, maxPeakProvider, myRendererSettings);
        using (MemoryStream ms = new MemoryStream())
        {
            image.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
            var imageBytes = ms.ToArray();
            var imgBse64 = System.Convert.ToBase64String(imageBytes);
            imageBase64 = imgBse64;
        }
        image.Dispose();
        return imageBase64;

    }
}

