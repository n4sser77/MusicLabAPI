namespace HttpServer.asp.Services.Interfaces
{
    public interface IWaveformGeneratorService
    {
        Task<string> GenerateWaveformImage(string filepath);
        Task<string> GenerateWaveformImage(byte[] bytes);
    }

    
}