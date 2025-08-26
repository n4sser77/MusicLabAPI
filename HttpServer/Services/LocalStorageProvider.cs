
using HttpServer.asp.Services;

public sealed class LocalStorageProvider : IStorageProvider
{
    private readonly IWebHostEnvironment _env;
    private readonly IConfiguration _config;
    private readonly SignedUrlService _urlService;

    private readonly string USER_DIRECTORY_PREFIX;
    private readonly string UPLOADS_DIR;

    public LocalStorageProvider(IWebHostEnvironment env, SignedUrlService urlService, IConfiguration config)
    {
        _env = env;
        _urlService = urlService;
        _config = config;

        // Read values from appsettings.json
        USER_DIRECTORY_PREFIX = Path.GetRelativePath(_env.ContentRootPath, config["Storage:UploadsDirPrefix"] ?? "user_");
        UPLOADS_DIR = Path.GetRelativePath(_env.ContentRootPath, _config["Storage:UploadsDir"] ?? "../data/uploads");


    }


    /// <summary>
    /// Gets user directory in blob service.
    /// </summary>
    /// <param name="userId">Defines the directory name for the user uploaded files</param>
    /// <returns>
    /// A tupple containing three string values: 
    /// 
    /// string prefixedUserDir, 
    /// string UPLOADS_DIR of the service,
    /// string userDir finalized directory of where the user files is
    /// </returns>
    private (string prefixedUserDir, string userDir) GetUserDir(int userId)
    {
        string prefixedUserDir = USER_DIRECTORY_PREFIX + userId;

        string userDir = Path.Combine(UPLOADS_DIR, prefixedUserDir);

        return (prefixedUserDir, userDir);
    }

    public Task<bool> DeleteAsync(string fileId, int userId)
    {



        // sanatizes the fileid by getting only the filename
        string safeFilename = Path.GetFileName(fileId);

        (string prefixedUserDir, string userDir) = GetUserDir(userId);

        if (!Directory.Exists(userDir))
        {
            return Task.FromResult(false);
        }


        string filePath = Path.Combine(userDir, safeFilename);
        if (!File.Exists(filePath))
        {
            return Task.FromResult(false);
        }

        try
        {

            File.Delete(filePath);
            return Task.FromResult(true);
        }
        catch (System.Exception)
        {
            return Task.FromResult(false);

        }


    }

    public Task<Stream?> DownloadAsync(string fileId, int userId)
    {
        (string prefixedUserDir, string userDir) = GetUserDir(userId);
        // sanatizes the fileid by getting only the filename
        string safeFilename = Path.GetFileName(fileId);

        if (!Directory.Exists(userDir))
        {
            return Task.FromResult<Stream?>(null);
        }
        string filePath = Path.Combine(userDir, safeFilename);
        if (!File.Exists(filePath))
        {
            return Task.FromResult<Stream?>(null);
        }

        var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);


        return Task.FromResult((Stream?)fileStream);

    }

    public Task<bool> ExistsAsync(string fileId, int userId)
    {
        (string prefixedUserDir, string userDir) = GetUserDir(userId);
        // sanatizes the fileid by getting only the filename
        string filename = Path.GetFileName(fileId);

        if (!Directory.Exists(userDir))
        {
            return Task.FromResult(false);
        }

        // Combine the file path
        string safeFilePath = Path.Combine(userDir, filename);

        // Check if the file exists
        var exists = File.Exists(safeFilePath);
        return Task.FromResult(exists);
    }

    public Task<bool> UpdateAsync(string fileId, string newFileId, int userId)
    {
        (string prefixedUserDir, string userDir) = GetUserDir(userId);
        var filepath = Path.Combine(userDir, fileId);
        var newFilepath = Path.Combine(userDir, newFileId);

        try
        {

            File.Move(filepath, newFilepath);
        }
        catch (System.Exception)
        {

            return Task.FromResult(false);
        }

        return Task.FromResult(true);
    }


    public async Task<string> UploadAsync(Stream fileStream, string originalFileName, string contentType, int userId)
    {
        (string prefixedUserDir, string userDir) = GetUserDir(userId);

        if (!Directory.Exists(UPLOADS_DIR))
        {
            Directory.CreateDirectory(UPLOADS_DIR);
        }

        // use user directory if exists else create 
        if (!Directory.Exists(userDir))
        {
            Directory.CreateDirectory(userDir);
        }



        var safeFilename = Path.GetFileName(originalFileName);
        var filepath = Path.Combine(userDir, safeFilename);

        // handle if there is already a file with the same name
        string filenameWithOutExtension = Path.GetFileNameWithoutExtension(filepath);
        string fileExtension = Path.GetExtension(filepath);
        int count = 1;
        while (File.Exists(filepath))
        {
            string newFilename = filenameWithOutExtension + "_" + count++ + fileExtension;
            filepath = Path.Combine(userDir, newFilename);
        }



        using (var stream = new FileStream(filepath, FileMode.Create))
        {
            await fileStream.CopyToAsync(stream);

        }
        // return a relative path /uploads/user_1/original.mp3
        var relativePath = Path.Combine(prefixedUserDir, Path.GetFileName(filepath)).Replace("\\", "/");
        return relativePath; //safe to return


    }

    public string GetAbsolutePath(string relativePath)
    {
        if (string.IsNullOrEmpty(relativePath))
            throw new ArgumentException("Path cannot be null or empty", nameof(relativePath));

        // Normalize slashes, remove leading slash to prevent Path.Combine ignoring base
        string cleanedPath = relativePath
            .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            .Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);

        // Combine with base uploads directory
        string baseUploadsPath = Path.Combine(_env.ContentRootPath, UPLOADS_DIR);

        string fullPath = Path.Combine(baseUploadsPath, cleanedPath);

        // Return the absolute normalized path
        return Path.GetFullPath(fullPath);
    }
    public string GetRelativeFilePath(int userId, string fileName)
    {
        string prefixedUserDir = USER_DIRECTORY_PREFIX + userId; // e.g. "user_6"
        string safeFileName = Path.GetFileName(fileName); // sanitize filename

        // Combine user directory prefix with filename using forward slashes (URL-friendly)
        string relativePath = Path.Combine(prefixedUserDir, safeFileName).Replace("\\", "/");

        return relativePath; // e.g. "user_6/filename.mp3"
    }

    public string GetSignedUrlAsync(string fileName, int userId, TimeSpan timeSpan, string baseUrl)
    {
        var url = _urlService.GenerateSignedUrl(userId, fileName, baseUrl);
        return url;
    }
}
