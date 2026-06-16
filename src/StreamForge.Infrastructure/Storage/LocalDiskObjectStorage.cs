using StreamForge.Core.Storage;

namespace StreamForge.Infrastructure.Storage;

public sealed class LocalDiskObjectStorage : IObjectStorage
{
    private readonly HttpClient _http;
    private readonly string _rootPath;
    private readonly string _baseUrl;

    public LocalDiskObjectStorage(IHttpClientFactory httpClientFactory, string rootPath, string baseUrl)
    {
        _http = httpClientFactory.CreateClient(nameof(LocalDiskObjectStorage));
        _rootPath = Path.GetFullPath(rootPath);
        _baseUrl = baseUrl.TrimEnd('/');
    }

    public async Task<string> DownloadAsync(string sourceUrl, string destinationDir, CancellationToken ct = default)
    {
        Directory.CreateDirectory(destinationDir);

        if (sourceUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            sourceUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            var fileName = Path.GetFileName(new Uri(sourceUrl).LocalPath);
            if (string.IsNullOrEmpty(fileName)) fileName = "source.mp4";
            var destPath = Path.Combine(destinationDir, fileName);

            using var response = await _http.GetAsync(sourceUrl, HttpCompletionOption.ResponseHeadersRead, ct);
            response.EnsureSuccessStatusCode();
            await using var fs = File.Create(destPath);
            await response.Content.CopyToAsync(fs, ct);
            return destPath;
        }

        // Treat as a local file path
        var dest = Path.Combine(destinationDir, Path.GetFileName(sourceUrl));
        File.Copy(sourceUrl, dest, overwrite: true);
        return dest;
    }

    public Task<string> UploadDirectoryAsync(string localDir, string keyPrefix, CancellationToken ct = default)
    {
        var destRoot = Path.Combine(_rootPath, keyPrefix.Replace('/', Path.DirectorySeparatorChar));

        foreach (var file in Directory.EnumerateFiles(localDir, "*", SearchOption.AllDirectories))
        {
            var relPath = Path.GetRelativePath(localDir, file);
            var destPath = Path.Combine(destRoot, relPath);
            Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);
            File.Copy(file, destPath, overwrite: true);
        }

        return Task.FromResult($"{_baseUrl}/{keyPrefix}/master.m3u8");
    }
}
