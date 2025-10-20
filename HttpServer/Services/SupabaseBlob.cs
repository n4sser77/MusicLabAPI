using Microsoft.AspNetCore.Routing.Constraints;
using Supabase.Storage; // Added for exception handling in UpdateAsync, if needed
using System;         // Added for TimeSpan
using System.IO;          // Added for Path, Stream, MemoryStream, SeekOrigin
using System.Threading.Tasks; // Added for Task
using System.Collections.Generic; // Added for HashSet
using Supabase;         // Added for Supabase.Client

namespace MusiclabApi.Services
{
    public class SupabaseBlob : IStorageProvider
    {
        private readonly Supabase.Client _supabase;
        private readonly string bucketName = "music-files";
        private readonly HashSet<string> allowedContentTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "audio/mpeg",    // .mp3
        "audio/wav",     // .wav
        "audio/x-wav",
        "audio/m4a",     // .m4a
        "audio/x-m4a",
        "audio/mp4",
        "audio/flac",    // .flac
        "audio/ogg",     // .ogg
        "audio/aac"      // .aac
        // Add any other specific types you want to support
    };

        public SupabaseBlob(Supabase.Client supabase)
        {
            _supabase = supabase;

        }
        public async Task<bool> DeleteAsync(string fileId, int userId)
        {
            string filePath = GetUserDir(fileId, userId);
            var file = await _supabase.Storage.From(bucketName).Remove(filePath);

            if (file is null)
            {
                return false;
            }

            return true;
        }

        private static string GetUserDir(string fileName, int userId)
        {
            // 1. Get just the file name (e.g., "February_14th_100BPM.m4a")
            var filename = Path.GetFileName(fileName);

            // 2. The desired path format is "user-{userId}/{filename}". 
            // Since we know Supabase and URLs use '/', just construct the string directly.
            // If you use Path.Combine, you MUST do the Replace('\\', '/') afterwards.
            var cleanFilename = filename?.Trim() ?? throw new ArgumentException("Filename cannot be null or empty.");
            // Simpler, safer, and clearer approach:
            return $"user-{userId}/{filename}";
        }

        public async Task<Stream?> DownloadAsync(string fileId, int userId)
        {

            var filePath = GetUserDir(fileId, userId);

            var bytes = await _supabase.Storage.From(bucketName).Download(filePath, onProgress: null);
            var ms = new MemoryStream(bytes);

            var fs = ms as Stream;
            return fs;
        }

        public async Task<bool> ExistsAsync(string fileId, int userId)
        {
            var filePath = GetUserDir(fileId, userId);

            var bytes = await _supabase.Storage.From(bucketName).Download(filePath, onProgress: null);

            return bytes is not null ? true : false;
        }

        // --- IMPLEMENTED METHOD ---
        public string GetAbsolutePath(string relativePath)
        {
            // Returns the public URL for the given file path in the bucket.
            // Note: This URL will only work if the file/bucket is public.
            // For private files, use GetSignedUrlAsync.
            return _supabase.Storage.From(bucketName).GetPublicUrl(relativePath);
        }

        // --- IMPLEMENTED METHOD ---
        public string GetRelativeFilePath(int userId, string fileName)
        {
            // Re-uses the existing helper method to ensure path consistency.
            return GetUserDir(fileName, userId);
        }

        // --- IMPLEMENTED METHOD ---
        public async Task<string> GetSignedUrlAsync(string fileName, int userId, TimeSpan timeSpan, string baseUrl)
        {
            // baseUrl is not needed as the Supabase client generates the full URL.
            string filePath = GetUserDir(fileName, userId);
            int expiresIn = (int)timeSpan.TotalSeconds;

            // The Supabase.Storage.CreateSignedUrl method is synchronous in the C# client.
            var url = await _supabase.Storage.From(bucketName).
                CreateSignedUrl(filePath, expiresIn);
            return url;
        }

        // --- IMPLEMENTED METHOD ---
        public async Task<bool> UpdateAsync(string fileId, string newFileId, int userId)
        {
            try
            {
                string oldPath = GetUserDir(fileId, userId);
                string newPath = GetUserDir(newFileId, userId);

                // In Supabase storage, "updating" a file ID (name) is a "Move" operation.
                await _supabase.Storage.From(bucketName).Move(oldPath, newPath);

                // If the Move operation succeeds, it returns without throwing.
                return true;
            }
            catch (Exception)
            {
                // If the Move operation fails (e.g., file not found, permissions error),
                // the client library throws an exception.
                // You might want to log the exception here.
                return false;
            }
        }

        public async Task<string> UploadAsync(Stream fileStream, string originalFileName, string contentType, int userId)
        {

            try
            {

                // 2. Validate the incoming content type
                if (!allowedContentTypes.Contains(contentType))
                {
                    // Throw an exception or return an error if the file type is not supported
                    throw new NotSupportedException($"Unsupported content type: {contentType}. Only audio files are allowed.");
                }


                var pathInBucket = GetUserDir(originalFileName, userId);
                byte[] bytes = await ReadStreamToEndAsync(fileStream);

                await _supabase.Storage
                    .From(bucketName)
                    .Upload(bytes, pathInBucket);

                //new()
                //{
                //    ContentType = contentType
                //}


                return pathInBucket;
            }
            catch (Exception)
            {

                throw;
            }
        }
        /// <summary>
        /// Helper method to asynchronously read the contents of a stream into a byte array.
        /// </summary>
        public static async Task<byte[]> ReadStreamToEndAsync(Stream stream)
        {
            // Ensure the stream is at the beginning before reading
            if (stream.CanSeek)
            {
                stream.Seek(0, SeekOrigin.Begin);
            }

            // Read all bytes using the MemoryStream approach
            using (var memoryStream = new MemoryStream())
            {
                await stream.CopyToAsync(memoryStream);
                return memoryStream.ToArray();
            }
        }
    }
}