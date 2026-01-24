#nullable enable

using System;
using System.IO;
using System.Net.Http;

namespace SAM.API.Utilities
{
    /// <summary>
    /// Provides helper methods for stream operations with size limits.
    /// </summary>
    public static class StreamHelper
    {
        /// <summary>
        /// Default buffer size for stream reading operations (80 KB).
        /// </summary>
        public const int DefaultBufferSize = 81920;

        /// <summary>
        /// Reads all bytes from a stream with a maximum size limit.
        /// </summary>
        /// <param name="stream">The stream to read from.</param>
        /// <param name="maxBytes">Maximum number of bytes allowed.</param>
        /// <param name="bufferSize">Buffer size for reading. Defaults to 80 KB.</param>
        /// <returns>The bytes read from the stream.</returns>
        /// <exception cref="ArgumentNullException">Thrown when stream is null.</exception>
        /// <exception cref="HttpRequestException">Thrown when the stream exceeds the maximum allowed size.</exception>
        public static byte[] ReadWithLimit(Stream stream, int maxBytes, int bufferSize = DefaultBufferSize)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            using MemoryStream memory = new();
            byte[] buffer = new byte[bufferSize];
            int read;
            int total = 0;
            while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
            {
                total += read;
                if (total > maxBytes)
                {
                    throw new HttpRequestException("Response exceeded maximum allowed size");
                }
                memory.Write(buffer, 0, read);
            }
            return memory.ToArray();
        }
    }
}
