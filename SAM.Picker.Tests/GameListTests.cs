using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Text;
using SAM.Picker;
using Xunit;

public class GameListTests
{
    [Fact]
    public void UsesLocalFileWhenNetworkFails()
    {
        string temp = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(temp);
        string local = Path.Combine(temp, "games.xml");
        File.WriteAllText(local, "<games><game type='normal'>1</game></games>");

        using HttpClient client = new(new FailingHandler());
        byte[] bytes = GameList.Load(temp, client, out bool usedLocal);

        Assert.True(usedLocal);
        Assert.NotNull(bytes);
    }

    [Fact]
    public void ThrowsWhenAllSourcesFail()
    {
        string temp = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        // no directory created, so no local file
        using HttpClient client = new(new FailingHandler());
        Assert.Throws<InvalidOperationException>(() =>
            GameList.Load(temp, client, out _));
    }

    [Fact]
    public void RejectsOversizedNetworkResponse()
    {
        string temp = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(temp);
        string local = Path.Combine(temp, "games.xml");
        File.WriteAllText(local, "<games><game type='normal'>1</game></games>");

        using HttpClient client = new(new OversizedHandler());
        byte[] bytes = GameList.Load(temp, client, out bool usedLocal);

        Assert.True(usedLocal);
        Assert.NotNull(bytes);
    }

    [Fact]
    public void RejectsCorruptedNetworkFile()
    {
        string temp = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(temp);
        string local = Path.Combine(temp, "games.xml");
        File.WriteAllText(local, "<games><game type='normal'>1</game></games>");

        using HttpClient client = new(new CorruptedHandler());
        byte[] bytes = GameList.Load(temp, client, out bool usedLocal);

        Assert.True(usedLocal);
        Assert.NotNull(bytes);
    }

    [Fact]
    public void RejectsExternalEntityReferences()
    {
        string temp = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(temp);
        string local = Path.Combine(temp, "games.xml");
        File.WriteAllText(local, "<games><game type='normal'>1</game></games>");

        using HttpClient client = new(new ExternalEntityHandler());
        byte[] bytes = GameList.Load(temp, client, out bool usedLocal);

        Assert.True(usedLocal);
        Assert.NotNull(bytes);
    }

    [Fact]
    public void HonorsHttpClientTimeout()
    {
        string temp = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        using HttpClient client = new(new SlowHandler())
        {
            Timeout = TimeSpan.FromMilliseconds(100),
        };

        var sw = System.Diagnostics.Stopwatch.StartNew();
        Assert.Throws<InvalidOperationException>(() => GameList.Load(temp, client, out _));
        sw.Stop();

        Assert.True(sw.Elapsed < TimeSpan.FromSeconds(1));
    }

    private sealed class FailingHandler : HttpMessageHandler
    {
        protected override System.Threading.Tasks.Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            throw new HttpRequestException("fail");
        }
    }

    private sealed class OversizedHandler : HttpMessageHandler
    {
        protected override System.Threading.Tasks.Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            byte[] data = new byte[GameList.MaxDownloadBytes + 1];
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
        protected override System.Threading.Tasks.Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
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
        protected override System.Threading.Tasks.Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
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
        protected override async System.Threading.Tasks.Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            await System.Threading.Tasks.Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(Array.Empty<byte>()),
            };
        }
    }
}
