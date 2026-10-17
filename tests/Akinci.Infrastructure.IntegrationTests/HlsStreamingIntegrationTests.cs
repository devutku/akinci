using System.Net;
using System.Security.Cryptography;
using System.Text;
using Akinci.Application.Ports;
using Akinci.Domain.Entities;
using Akinci.Domain.ValueObjects;
using Akinci.Infrastructure.Hls;
using Xunit;

namespace Akinci.Infrastructure.IntegrationTests;

public class HlsStreamingIntegrationTests : IAsyncLifetime
{
    private readonly HttpListener _listener;
    private readonly string _serverPrefix;
    private readonly CancellationTokenSource _serverCts;
    private Task? _serverTask;

    private readonly byte[] _segment1Data;
    private readonly byte[] _segment2Data;
    private readonly byte[] _segment3Data;

    public HlsStreamingIntegrationTests()
    {
        // 3 adet test video segmenti verisi üret
        _segment1Data = Encoding.UTF8.GetBytes("---TS-SEGMENT-1-MOCK-DATA---");
        _segment2Data = Encoding.UTF8.GetBytes("---TS-SEGMENT-2-MOCK-DATA---");
        _segment3Data = Encoding.UTF8.GetBytes("---TS-SEGMENT-3-MOCK-DATA---");

        using var tempSocket = new System.Net.Sockets.TcpListener(IPAddress.Loopback, 0);
        tempSocket.Start();
        int port = ((IPEndPoint)tempSocket.LocalEndpoint).Port;
        tempSocket.Stop();

        _serverPrefix = $"http://127.0.0.1:{port}/";
        _listener = new HttpListener();
        _listener.Prefixes.Add(_serverPrefix);
        _serverCts = new CancellationTokenSource();
    }

    public Task InitializeAsync()
    {
        _listener.Start();
        _serverTask = Task.Run(() => RunServerAsync(_serverCts.Token));
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        _serverCts.Cancel();
        _listener.Stop();
        _listener.Close();
        if (_serverTask != null)
        {
            try { await _serverTask; } catch { }
        }
    }

    private async Task RunServerAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested && _listener.IsListening)
        {
            try
            {
                var context = await _listener.GetContextAsync();
                var path = context.Request.Url?.AbsolutePath ?? "";

                if (path == "/master.m3u8")
                {
                    string master = @"#EXTM3U
#EXT-X-STREAM-INF:BANDWIDTH=1280000,RESOLUTION=720x480
720p.m3u8
#EXT-X-STREAM-INF:BANDWIDTH=5000000,RESOLUTION=1920x1080
1080p.m3u8";
                    byte[] bytes = Encoding.UTF8.GetBytes(master);
                    context.Response.ContentType = "application/vnd.apple.mpegurl";
                    context.Response.ContentLength64 = bytes.Length;
                    await context.Response.OutputStream.WriteAsync(bytes, ct);
                    context.Response.Close();
                }
                else if (path == "/1080p.m3u8")
                {
                    string media = @"#EXTM3U
#EXT-X-TARGETDURATION:4
#EXTINF:4.0,
seg1.ts
#EXTINF:4.0,
seg2.ts
#EXTINF:2.5,
seg3.ts
#EXT-X-ENDLIST";
                    byte[] bytes = Encoding.UTF8.GetBytes(media);
                    context.Response.ContentType = "application/vnd.apple.mpegurl";
                    context.Response.ContentLength64 = bytes.Length;
                    await context.Response.OutputStream.WriteAsync(bytes, ct);
                    context.Response.Close();
                }
                else if (path == "/seg1.ts")
                {
                    context.Response.ContentType = "video/mp2t";
                    context.Response.ContentLength64 = _segment1Data.Length;
                    await context.Response.OutputStream.WriteAsync(_segment1Data, ct);
                    context.Response.Close();
                }
                else if (path == "/seg2.ts")
                {
                    context.Response.ContentType = "video/mp2t";
                    context.Response.ContentLength64 = _segment2Data.Length;
                    await context.Response.OutputStream.WriteAsync(_segment2Data, ct);
                    context.Response.Close();
                }
                else if (path == "/seg3.ts")
                {
                    context.Response.ContentType = "video/mp2t";
                    context.Response.ContentLength64 = _segment3Data.Length;
                    await context.Response.OutputStream.WriteAsync(_segment3Data, ct);
                    context.Response.Close();
                }
                else
                {
                    context.Response.StatusCode = 404;
                    context.Response.Close();
                }
            }
            catch (HttpListenerException) { break; }
            catch (Exception) { }
        }
    }

    [Fact]
    public void HlsPlaylistParser_ParsesMasterPlaylistCorrectly()
    {
        string m3u8 = @"#EXTM3U
#EXT-X-STREAM-INF:BANDWIDTH=800000,RESOLUTION=640x360
360p/index.m3u8
#EXT-X-STREAM-INF:BANDWIDTH=3000000,RESOLUTION=1280x720
720p/index.m3u8
#EXT-X-STREAM-INF:BANDWIDTH=6000000,RESOLUTION=1920x1080
1080p/index.m3u8";

        var baseUri = new Uri("https://video.example.com/live/master.m3u8");
        var playlist = HlsPlaylistParser.Parse(m3u8, baseUri);

        Assert.True(playlist.IsMaster);
        Assert.Equal(3, playlist.Variants.Count);
        // En yüksek çözünürlük başta olmalı
        Assert.Equal(6000000, playlist.Variants[0].Bandwidth);
        Assert.Equal("1920x1080", playlist.Variants[0].Resolution);
        Assert.Equal("https://video.example.com/live/1080p/index.m3u8", playlist.Variants[0].Url);
    }

    [Fact]
    public void HlsPlaylistParser_ParsesMediaPlaylistWithRelativeUrlsAndVOD()
    {
        string m3u8 = @"#EXTM3U
#EXT-X-TARGETDURATION:6
#EXTINF:6.0,
chunk_001.ts
#EXTINF:5.5,
chunk_002.ts
#EXT-X-ENDLIST";

        var baseUri = new Uri("https://stream.example.com/hls/playlist.m3u8");
        var playlist = HlsPlaylistParser.Parse(m3u8, baseUri);

        Assert.False(playlist.IsMaster);
        Assert.False(playlist.IsLive); // #EXT-X-ENDLIST var -> VOD
        Assert.True(playlist.HasEndList);
        Assert.Equal(2, playlist.Segments.Count);
        Assert.Equal("https://stream.example.com/hls/chunk_001.ts", playlist.Segments[0].Url);
        Assert.Equal(6.0, playlist.Segments[0].DurationSeconds);
        Assert.Equal("https://stream.example.com/hls/chunk_002.ts", playlist.Segments[1].Url);
        Assert.Equal(5.5, playlist.Segments[1].DurationSeconds);
        Assert.Equal(11.5, playlist.TotalDurationSeconds);
    }

    [Fact]
    public void DownloadUrl_CorrectlyDetectsHlsAndDerivesTsFileName()
    {
        var hlsUrl1 = DownloadUrl.Create("https://kick.com/api/stream/master.m3u8");
        var hlsUrl2 = DownloadUrl.Create("https://video.cdn.com/stream?source=live.m3u8&auth=token123");
        var normalUrl = DownloadUrl.Create("https://example.com/archive.zip");

        Assert.True(hlsUrl1.IsHls);
        Assert.True(hlsUrl2.IsHls);
        Assert.False(normalUrl.IsHls);

        Assert.Equal("master.ts", hlsUrl1.GetFileNameFromUrl());
        Assert.Equal("archive.zip", normalUrl.GetFileNameFromUrl());
    }

    [Fact]
    public async Task HlsDownloader_DownloadsAllSegmentsAndAssemblesExactData()
    {
        var masterUrl = DownloadUrl.Create($"{_serverPrefix}master.m3u8");

        var metaService = new HlsMetadataService();
        var metadata = await metaService.GetMetadataAsync(masterUrl);

        Assert.NotNull(metadata);
        Assert.Equal("master.ts", metadata.SuggestedFileName);

        var downloader = new HlsChunkDownloader();
        var chunk = new Chunk(Guid.NewGuid(), 0, new ByteRange(0, long.MaxValue - 1));

        using var memoryStream = new MemoryStream();

        await downloader.DownloadChunkAsync(
            masterUrl,
            chunk,
            async (offset, data, ct) =>
            {
                memoryStream.Seek(offset, SeekOrigin.Begin);
                await memoryStream.WriteAsync(data, ct);
            });

        Assert.True(chunk.IsCompleted);

        // Tüm segmentlerin sırayla tek bir stream içinde birleştiğini doğrula
        var expectedBytes = _segment1Data
            .Concat(_segment2Data)
            .Concat(_segment3Data)
            .ToArray();

        var actualBytes = memoryStream.ToArray();

        Assert.Equal(expectedBytes.Length, actualBytes.Length);
        Assert.Equal(expectedBytes, actualBytes);
    }
}
