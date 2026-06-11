using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using SAM.WinForms;
using Xunit;

// Guards the recurring "SAM.Picker won't close while downloading icons" bug. The
// root cause was that the HTTP response body was read synchronously and could not
// be cancelled (HttpClient.Timeout does not cover the stream under
// ResponseHeadersRead), so a stalled connection blocked forever and the form's
// Dispose wait spun indefinitely. These tests pin the contract that the download
// honors cancellation mid-body and enforces its size limit.
public class ImageDownloaderTests
{
    private const int MaxBytes = 4 * 1024 * 1024;

    [Fact]
    public async Task DownloadImageDataAsync_HonorsCancellationDuringStalledBodyRead()
    {
        using var cts = new CancellationTokenSource();
        using var syncReadRelease = new ManualResetEventSlim(false);
        var handler = new StallingBodyHandler(syncReadRelease);
        using var client = new HttpClient(handler);

        try
        {
            var download = ImageDownloader.DownloadImageDataAsync(
                new Uri("https://example.invalid/logo.jpg"), client, MaxBytes, cts.Token);

            // Wait until the body read is actually in progress, then cancel like the
            // form does on close.
            await handler.ReadStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
            cts.Cancel();

            // Cancellation must unblock the read promptly. If the body were read
            // synchronously (the old, twice-seen bug), the request would hang and
            // WaitAsync would throw TimeoutException instead of OperationCanceled.
            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                async () => await download.WaitAsync(TimeSpan.FromSeconds(5)));
        }
        finally
        {
            syncReadRelease.Set(); // release the sync-read thread if it was used
        }
    }

    [Fact]
    public async Task DownloadImageDataAsync_RejectsBodyExceedingLimit()
    {
        // No Content-Length (non-seekable stream), so the cap must be enforced while
        // streaming the body, not just from the header.
        using var client = new HttpClient(new StreamedBodyHandler(byteCount: 4096));

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            ImageDownloader.DownloadImageDataAsync(
                new Uri("https://example.invalid/logo.jpg"), client, maxBytes: 1024));
    }

    [Fact]
    public async Task DownloadImageDataAsync_RejectsOversizedContentLength()
    {
        using var client = new HttpClient(new ContentLengthHandler(byteCount: 2048));

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            ImageDownloader.DownloadImageDataAsync(
                new Uri("https://example.invalid/logo.jpg"), client, maxBytes: 1024));
    }

    private sealed class StallingBodyHandler : HttpMessageHandler
    {
        public readonly TaskCompletionSource ReadStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly ManualResetEventSlim _syncReadRelease;

        public StallingBodyHandler(ManualResetEventSlim syncReadRelease)
        {
            this._syncReadRelease = syncReadRelease;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StreamContent(new StallingStream(this.ReadStarted, this._syncReadRelease)),
            };
            response.Content.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
            return Task.FromResult(response);
        }
    }

    private sealed class StallingStream : Stream
    {
        private readonly TaskCompletionSource _readStarted;
        private readonly ManualResetEventSlim _syncReadRelease;

        public StallingStream(TaskCompletionSource readStarted, ManualResetEventSlim syncReadRelease)
        {
            this._readStarted = readStarted;
            this._syncReadRelease = syncReadRelease;
        }

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            this._readStarted.TrySetResult();
            // A correct (cancellable) body read aborts here when the token fires.
            await Task.Delay(Timeout.Infinite, cancellationToken).ConfigureAwait(false);
            return 0;
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return this.ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            // The old uncancellable path: signal, then block ignoring the token. If
            // production reverts to a synchronous read, the download hangs here and
            // the test's WaitAsync times out -> the regression is caught.
            this._readStarted.TrySetResult();
            this._syncReadRelease.Wait();
            return 0;
        }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override void Flush() { }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }

    private sealed class StreamedBodyHandler : HttpMessageHandler
    {
        private readonly int _byteCount;

        public StreamedBodyHandler(int byteCount)
        {
            this._byteCount = byteCount;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StreamContent(new NonSeekableStream(this._byteCount)),
            };
            response.Content.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
            return Task.FromResult(response);
        }
    }

    private sealed class ContentLengthHandler : HttpMessageHandler
    {
        private readonly int _byteCount;

        public ContentLengthHandler(int byteCount)
        {
            this._byteCount = byteCount;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(new byte[this._byteCount]),
            };
            response.Content.Headers.ContentLength = this._byteCount;
            response.Content.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
            return Task.FromResult(response);
        }
    }

    // A non-seekable read-only stream so StreamContent leaves Content-Length null,
    // forcing the size cap to be enforced while reading the body.
    private sealed class NonSeekableStream : Stream
    {
        private readonly byte[] _data;
        private int _pos;

        public NonSeekableStream(int size)
        {
            this._data = new byte[size];
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int remaining = this._data.Length - this._pos;
            if (remaining <= 0)
            {
                return 0;
            }
            int n = Math.Min(count, Math.Min(remaining, 4096));
            Array.Copy(this._data, this._pos, buffer, offset, n);
            this._pos += n;
            return n;
        }

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            int remaining = this._data.Length - this._pos;
            if (remaining <= 0)
            {
                return new ValueTask<int>(0);
            }
            int n = Math.Min(buffer.Length, Math.Min(remaining, 4096));
            this._data.AsMemory(this._pos, n).CopyTo(buffer);
            this._pos += n;
            return new ValueTask<int>(n);
        }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override void Flush() { }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
