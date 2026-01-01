using System;
using System.Net;
using System.Net.Http;

namespace SAM.WinForms
{
    /// <summary>
    /// Manages a shared HttpClient instance for the application.
    /// Implements the singleton pattern to avoid socket exhaustion and improve performance.
    /// </summary>
    public static class HttpClientManager
    {
        private static readonly Lazy<HttpClient> _lazyClient = new(() => CreateHttpClient());

        /// <summary>
        /// Gets the shared HttpClient instance.
        /// </summary>
        public static HttpClient Client => _lazyClient.Value;

        /// <summary>
        /// Default timeout for HTTP requests in seconds.
        /// </summary>
        public const int DefaultTimeoutSeconds = 30;

        private static HttpClient CreateHttpClient()
        {
            // Configure SocketsHttpHandler for better connection management
            var handler = new SocketsHttpHandler
            {
                PooledConnectionLifetime = TimeSpan.FromMinutes(2), // Recycle connections every 2 minutes
                PooledConnectionIdleTimeout = TimeSpan.FromMinutes(1), // Close idle connections after 1 minute
                MaxConnectionsPerServer = 10, // Allow up to 10 concurrent connections per server
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate, // Support compression
            };

            var client = new HttpClient(handler, disposeHandler: false)
            {
                Timeout = TimeSpan.FromSeconds(DefaultTimeoutSeconds),
            };

            // Set default headers
            client.DefaultRequestHeaders.UserAgent.ParseAdd("SAM.X/1.0");
            client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate");

            return client;
        }

        /// <summary>
        /// Creates a new HttpClient instance with a custom timeout.
        /// Use this sparingly - prefer using the shared Client with CancellationToken for per-request timeouts.
        /// </summary>
        /// <param name="timeoutSeconds">Timeout in seconds</param>
        /// <returns>A new HttpClient instance (caller is responsible for disposal)</returns>
        public static HttpClient CreateWithTimeout(int timeoutSeconds)
        {
            var handler = new SocketsHttpHandler
            {
                PooledConnectionLifetime = TimeSpan.FromMinutes(2),
                PooledConnectionIdleTimeout = TimeSpan.FromMinutes(1),
                MaxConnectionsPerServer = 10,
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
            };

            var client = new HttpClient(handler, disposeHandler: true)
            {
                Timeout = TimeSpan.FromSeconds(timeoutSeconds),
            };

            client.DefaultRequestHeaders.UserAgent.ParseAdd("SAM.X/1.0");
            client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate");

            return client;
        }
    }
}
