using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using SAM.Picker;
using Xunit;

public class DownloadDataAsyncTests
{
    private static async Task<(byte[] Data, string ContentType)> DownloadDataAsync(HttpClient client, Uri uri)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, uri);
        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        var finalUri = response.RequestMessage?.RequestUri;
        if (finalUri == null || ImageUrlValidator.TryCreateUri(finalUri.ToString(), out _) == false)
        {
            throw new HttpRequestException("Response redirected to unapproved host");
        }

        var contentLength = response.Content.Headers.ContentLength;
        if (contentLength == null || contentLength.Value > 512 * 1024)
        {
            throw new HttpRequestException("Response too large or missing length");
        }

        var contentType = response.Content.Headers.ContentType?.MediaType ?? string.Empty;

        using var stream = await response.Content.ReadAsStreamAsync();
        var data = ReadWithLimit(stream, 512 * 1024);
        return (data, contentType);
    }

    private static byte[] ReadWithLimit(Stream stream, int maxBytes)
    {
        using MemoryStream memory = new();
        byte[] buffer = new byte[81920];
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

    private class StubHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;
        public StubHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
        {
            _handler = handler;
        }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(_handler(request));
        }
    }

    [Fact]
    public async Task RejectsRedirectResponse()
    {
        var handler = new StubHandler(req =>
        {
            var resp = new HttpResponseMessage(HttpStatusCode.Redirect);
            resp.Headers.Location = new Uri("https://example.com/logo.png");
            resp.RequestMessage = req;
            return resp;
        });
        var client = new HttpClient(handler);
        await Assert.ThrowsAsync<HttpRequestException>(() => DownloadDataAsync(client, new Uri("https://cdn.steamstatic.com/logo.png")));
    }

    [Fact]
    public async Task RejectsUnapprovedFinalHost()
    {
        var handler = new StubHandler(req =>
        {
            var resp = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(new byte[] { 1 })
            };
            resp.Content.Headers.ContentLength = 1;
            resp.Content.Headers.ContentType = new MediaTypeHeaderValue("image/png");
            resp.RequestMessage = new HttpRequestMessage(HttpMethod.Get, "https://example.com/logo.png");
            return resp;
        });
        var client = new HttpClient(handler);
        await Assert.ThrowsAsync<HttpRequestException>(() => DownloadDataAsync(client, new Uri("https://cdn.steamstatic.com/logo.png")));
    }

    [Fact]
    public async Task AllowsApprovedHost()
    {
        var handler = new StubHandler(req =>
        {
            var resp = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(new byte[] { 1, 2, 3 })
            };
            resp.Content.Headers.ContentLength = 3;
            resp.Content.Headers.ContentType = new MediaTypeHeaderValue("image/png");
            resp.RequestMessage = req;
            return resp;
        });
        var client = new HttpClient(handler);
        var (data, contentType) = await DownloadDataAsync(client, new Uri("https://cdn.steamstatic.com/logo.png"));
        Assert.Equal("image/png", contentType);
        Assert.Equal(new byte[] { 1, 2, 3 }, data);
    }
}
