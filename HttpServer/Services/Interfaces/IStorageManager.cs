


public interface IStorageProvider
{
    /// <summary>
    /// Uploads a file stream and returns a generated file ID or path.
    /// </summary>
    /// <param name="fileStream">The file stream to upload.</param>
    /// <param name="originalFileName">Original file name from the client.</param>
    /// <param name="contentType">MIME type (e.g., image/jpeg, audio/mp3).</param>
    /// <returns>Generated ID or file path.</returns>
    Task<string> UploadAsync(Stream fileStream, string originalFileName, string contentType, int userId);

    /// <summary>
    /// Downloads the file stream by its unique ID.
    /// </summary>
    /// <param name="fileId">The unique file ID or path used to retrieve the file.</param>
    /// <returns>The file stream.</returns>
    Task<Stream?> DownloadAsync(string fileId, int userId);

    /// <summary>
    /// Deletes a file by ID.
    /// </summary>
    /// <param name="fileId">The unique file ID or path to delete.</param>
    Task<bool> DeleteAsync(string fileId, int userId);

    /// <summary>
    /// Checks if a file exists by ID.
    /// </summary>
    Task<bool> ExistsAsync(string fileId, int userId);
    /// <summary>
    /// Updates a files name
    /// </summary>
    /// <param name="fileId"></param>
    /// <param name="newFileId"></param>
    /// <param name="userId"></param>
    /// <returns></returns>
    Task<bool> UpdateAsync(string fileId, string newFileId, int userId);
    string GetAbsolutePath(string relativePath);
    string GetRelativeFilePath(int userId, string fileName);
    string GetSignedUrlAsync(string fileName, int userId, TimeSpan timeSpan, string baseUrl);
}
