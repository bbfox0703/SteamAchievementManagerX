using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Text;
using SAM.API.Constants;
using SAM.Picker;
using Xunit;

public class GameListTests : IDisposable
{
    private readonly List<string> _tempDirs = new();

    // Creates a temp directory containing a games.xml that is older than the
    // 30-minute cache window, so GameList.Load actually attempts the network
    // instead of short-circuiting to a fresh local file (which would never
    // invoke the HttpMessageHandler and leave the network path untested).
    private string CreateStaleLocalGames()
    {
        string temp = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        this._tempDirs.Add(temp);
        Directory.CreateDirectory(temp);
        string local = Path.Combine(temp, "games.xml");
        File.WriteAllText(local, "<games><game type='normal'>1</game></games>");
        File.SetLastWriteTimeUtc(local, DateTime.UtcNow - TimeSpan.FromHours(1));
        return temp;
    }

    public void Dispose()
    {
        foreach (var dir in this._tempDirs)
        {
            try
            {
                Directory.Delete(dir, recursive: true);
            }
            catch
            {
                // best-effort cleanup
            }
        }
    }

    [Fact]
    public void UsesLocalFileWhenNetworkFails()
    {
        string temp = CreateStaleLocalGames();

        var handler = new FailingHandler();
        using HttpClient client = new(handler);
        byte[] bytes = GameList.Load(temp, client, out bool usedLocal);

        Assert.True(handler.Invoked); // the network path was actually exercised
        Assert.True(usedLocal);
        Assert.NotNull(bytes);
    }

    [Fact]
    public void ThrowsWhenAllSourcesFail()
    {
        string temp = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        // no directory created, so no local file
        var handler = new FailingHandler();
        using HttpClient client = new(handler);
        Assert.Throws<InvalidOperationException>(() =>
            GameList.Load(temp, client, out _));
        Assert.True(handler.Invoked);
    }

    [Fact]
    public void RejectsOversizedNetworkResponse()
    {
        string temp = CreateStaleLocalGames();

        var handler = new OversizedHandler();
        using HttpClient client = new(handler);
        byte[] bytes = GameList.Load(temp, client, out bool usedLocal);

        // Oversized response is rejected, so Load falls back to the local file.
        Assert.True(handler.Invoked);
        Assert.True(usedLocal);
        Assert.NotNull(bytes);
    }

    [Fact]
    public void RejectsCorruptedNetworkFile()
    {
        string temp = CreateStaleLocalGames();

        var handler = new CorruptedHandler();
        using HttpClient client = new(handler);
        byte[] bytes = GameList.Load(temp, client, out bool usedLocal);

        // Non-XML response is rejected, so Load falls back to the local file.
        Assert.True(handler.Invoked);
        Assert.True(usedLocal);
        Assert.NotNull(bytes);
    }

    [Fact]
    public void RejectsExternalEntityReferences()
    {
        string temp = CreateStaleLocalGames();

        var handler = new ExternalEntityHandler();
        using HttpClient client = new(handler);
        byte[] bytes = GameList.Load(temp, client, out bool usedLocal);

        // The XXE payload must be rejected (DtdProcessing.Prohibit), so Load
        // falls back to the local file rather than returning the malicious doc.
        Assert.True(handler.Invoked);
        Assert.True(usedLocal);
        Assert.NotNull(bytes);
        Assert.DoesNotContain("root:", Encoding.UTF8.GetString(bytes));
    }

    [Fact]
    public void HonorsHttpClientTimeout()
    {
        string temp = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        var handler = new SlowHandler();
        using HttpClient client = new(handler)
        {
            Timeout = TimeSpan.FromMilliseconds(100),
        };

        var sw = System.Diagnostics.Stopwatch.StartNew();
        Assert.Throws<InvalidOperationException>(() => GameList.Load(temp, client, out _));
        sw.Stop();

        Assert.True(handler.Invoked);
        // 100 ms timeout against a 5 s handler: a generous bound that still proves
        // the timeout fired well before the handler's natural completion.
        Assert.True(sw.Elapsed < TimeSpan.FromSeconds(2));
    }

    private sealed class FailingHandler : HttpMessageHandler
    {
        public bool Invoked;

        protected override System.Threading.Tasks.Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            this.Invoked = true;
            throw new HttpRequestException("fail");
        }
    }

    private sealed class OversizedHandler : HttpMessageHandler
    {
        public bool Invoked;

        protected override System.Threading.Tasks.Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            this.Invoked = true;
            byte[] data = new byte[DownloadLimits.MaxGameListBytes + 1];
            var response = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(data),
            };
            response.Content.Headers.ContentLength = data.Length;
            response.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/xml");
            return System.Threading.Tasks.Task.FromResult(response);
        }
    }

    private sealed class CorruptedHandler : HttpMessageHandler
    {
        public bool Invoked;

        protected override System.Threading.Tasks.Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            this.Invoked = true;
            byte[] data = new byte[] { 1, 2, 3, 4 };
            var response = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(data),
            };
            response.Content.Headers.ContentLength = data.Length;
            response.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/xml");
            return System.Threading.Tasks.Task.FromResult(response);
        }
    }

    private sealed class ExternalEntityHandler : HttpMessageHandler
    {
        public bool Invoked;

        protected override System.Threading.Tasks.Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            this.Invoked = true;
            const string payload = "<?xml version='1.0'?><!DOCTYPE doc [<!ENTITY xxe SYSTEM \"file:///etc/passwd\">]><games>&xxe;</games>";
            byte[] data = Encoding.UTF8.GetBytes(payload);
            var response = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(data),
            };
            response.Content.Headers.ContentLength = data.Length;
            response.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/xml");
            return System.Threading.Tasks.Task.FromResult(response);
        }
    }

    private sealed class SlowHandler : HttpMessageHandler
    {
        public bool Invoked;

        protected override async System.Threading.Tasks.Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            this.Invoked = true;
            await System.Threading.Tasks.Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(Array.Empty<byte>()),
            };
        }
    }
}
