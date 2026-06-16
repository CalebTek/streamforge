using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using StreamForge.Core.Storage;

namespace StreamForge.Infrastructure.Storage;

public sealed class S3ObjectStorage : IObjectStorage, IDisposable
{
    private readonly AmazonS3Client _s3;
    private readonly HttpClient _http;
    private readonly string _bucket;
    private readonly string _baseUrl;

    public S3ObjectStorage(IHttpClientFactory httpClientFactory, string serviceUrl, string bucket,
        string region, string accessKey, string secretKey, bool pathStyle, string baseUrl)
    {
        _http = httpClientFactory.CreateClient(nameof(S3ObjectStorage));
        _bucket = bucket;
        _baseUrl = baseUrl.TrimEnd('/');

        var config = new AmazonS3Config { ForcePathStyle = pathStyle };
        if (!string.IsNullOrEmpty(serviceUrl))
            config.ServiceURL = serviceUrl;
        else
            config.RegionEndpoint = RegionEndpoint.GetBySystemName(region);

        _s3 = new AmazonS3Client(new BasicAWSCredentials(accessKey, secretKey), config);
    }

    public async Task<string> DownloadAsync(string sourceUrl, string destinationDir, CancellationToken ct = default)
    {
        Directory.CreateDirectory(destinationDir);
        var fileName = Path.GetFileName(new Uri(sourceUrl).LocalPath);
        if (string.IsNullOrEmpty(fileName)) fileName = "source.mp4";
        var destPath = Path.Combine(destinationDir, fileName);

        using var response = await _http.GetAsync(sourceUrl, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();
        await using var fs = File.Create(destPath);
        await response.Content.CopyToAsync(fs, ct);
        return destPath;
    }

    public async Task<string> UploadDirectoryAsync(string localDir, string keyPrefix, CancellationToken ct = default)
    {
        foreach (var file in Directory.EnumerateFiles(localDir, "*", SearchOption.AllDirectories))
        {
            var relPath = Path.GetRelativePath(localDir, file).Replace('\\', '/');
            var key = $"{keyPrefix}/{relPath}";

            await using var stream = File.OpenRead(file);
            await _s3.PutObjectAsync(new PutObjectRequest
            {
                BucketName = _bucket,
                Key = key,
                InputStream = stream,
                ContentType = GetContentType(file)
            }, ct);
        }

        return $"{_baseUrl}/{keyPrefix}/master.m3u8";
    }

    private static string GetContentType(string path) => Path.GetExtension(path).ToLowerInvariant() switch
    {
        ".m3u8" => "application/vnd.apple.mpegurl",
        ".ts"   => "video/mp2t",
        _       => "application/octet-stream"
    };

    public void Dispose() => _s3.Dispose();
}
