using HttpServer.asp.Services.Interfaces;
using NAudio.Wave;
using SkiaSharp;
using System.Diagnostics;
using System.IO;

namespace HttpServer.asp.Services;

public class WaveformServiceSkia : IWaveformGeneratorService
{
    private readonly IWebHostEnvironment _env;
    private readonly string UPLOADS_DIR;

    public WaveformServiceSkia(IWebHostEnvironment env)
    {
        _env = env;
        UPLOADS_DIR = Path.Combine(_env.WebRootPath ?? _env.ContentRootPath, "uploads");
    }

    public async Task<string> GenerateWaveformImage(string filepath)
    {
        string fullPath = Path.Combine(UPLOADS_DIR, filepath);
        if (!File.Exists(fullPath))
            throw new FileNotFoundException("Audio file not found", fullPath);

        var ffmpeg = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = $"-i \"{fullPath}\" -f wav -",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        ffmpeg.Start();

        using var ms = new MemoryStream();
        ffmpeg.StandardOutput.BaseStream.CopyTo(ms);
        ms.Position = 0;

        using var reader = new WaveFileReader(ms);

        int width = 710;
        int topHeight = 32;
        int bottomHeight = 32;
        int height = topHeight + bottomHeight;

        // Read all samples
        var sampleProvider = reader.ToSampleProvider();

        List<float> samples = new();
        float[] buffer = new float[reader.WaveFormat.SampleRate * reader.WaveFormat.Channels];
        int read;

        while ((read = sampleProvider.Read(buffer, 0, buffer.Length)) > 0)
        {
            samples.AddRange(buffer.Take(read));
        }


        // If the audio file is empty or too short, return an empty waveform
        if (samples.Count == 0)
        {
            using var emptySurface = SKSurface.Create(new SKImageInfo(width, height));
            using var emptyImg = emptySurface.Snapshot();
            using var emptyData = emptyImg.Encode(SKEncodedImageFormat.Png, 100);
            using var emptyMs = new MemoryStream();
            emptyData.SaveTo(emptyMs);
            return Convert.ToBase64String(emptyMs.ToArray());
        }

        // Find the maximum absolute sample value for normalization
        float max = samples.Max(s => Math.Abs(s));
        if (max > 0)
            samples = samples.Select(s => s / max).ToList();

        // SkiaSharp surface
        using var surface = SKSurface.Create(new SKImageInfo(width, height));
        var canvas = surface.Canvas;
        canvas.Clear(SKColors.Transparent);

        // Top paint (burnt orange, almost black)
        using var topPaint = new SKPaint
        {
            // Color = new SKColor(191, 54, 12), // #BF360C
            Color = new SKColor(111, 39, 17), // rgba(111, 39, 17, 1)
            IsAntialias = true,
            Style = SKPaintStyle.Fill
        };

        // Bottom paint (lighter orange)
        using var bottomPaint = new SKPaint
        {
            Color = new SKColor(244, 81, 30), // #F4511E
            IsAntialias = true,
            Style = SKPaintStyle.Fill
        };

        // Determine how many samples to process per pixel column to get smoother peaks
        int samplesPerPixel = Math.Max(1, samples.Count / width);

        for (int x = 0; x < width; x++)
        {
            int start = x * samplesPerPixel;
            int end = Math.Min(start + samplesPerPixel, samples.Count);

            if (end <= start) continue;

            // Aggregate peaks within the sample range
            float maxPeak = 0;
            float minPeak = 0;
            for (int i = start; i < end; i++)
            {
                if (samples[i] > maxPeak) maxPeak = samples[i];
                if (samples[i] < minPeak) minPeak = samples[i];
            }

            // Map peaks to canvas coordinates
            float topY = topHeight - (maxPeak * topHeight);
            float bottomY = topHeight - (minPeak * bottomHeight);

            // Draw filled rectangles instead of lines
            canvas.DrawRect(SKRect.Create(x, topY, 1, topHeight - topY), topPaint);
            canvas.DrawRect(SKRect.Create(x, topHeight, 1, bottomY - topHeight), bottomPaint);
        }

        // Export PNG as Base64
        using var img = surface.Snapshot();
        using var data = img.Encode(SKEncodedImageFormat.Png, 100);
        using var ms = new MemoryStream();
        data.SaveTo(ms);

        return Convert.ToBase64String(ms.ToArray());
    }
}