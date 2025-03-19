namespace HttpServer.asp.Services.Interfaces
{
    public interface IWaveformGeneratorService
    {
        Task<string> GenerateWaveformImage(string filepath);
    }
}