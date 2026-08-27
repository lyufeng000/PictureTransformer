using System.Buffers;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PictureTransformer.Services;

public sealed record UpdateInfo(
    Version Version,
    string TagName,
    string ReleaseNotes,
    Uri InstallerUri,
    string? InstallerDigest);

public interface IUpdateService
{
    Task<UpdateInfo?> CheckForUpdateAsync(Version currentVersion, CancellationToken cancellationToken = default);

    Task<string> DownloadInstallerAsync(
        UpdateInfo update,
        string? downloadDirectory = null,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default);
}

public sealed class GitHubUpdateService : IUpdateService
{
    private const string LatestReleaseUrl =
        "https://api.github.com/repos/lyufeng000/PictureTransformer/releases/latest";
    private const string InstallerAssetName = "PictureTransformer-Setup.exe";
    private static readonly TimeSpan CheckTimeout = TimeSpan.FromSeconds(10);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly HttpClient SharedHttpClient = CreateHttpClient();

    private readonly HttpClient _httpClient;

    public GitHubUpdateService() : this(SharedHttpClient)
    {
    }

    public GitHubUpdateService(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    public async Task<UpdateInfo?> CheckForUpdateAsync(
        Version currentVersion,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(currentVersion);

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(CheckTimeout);

        using HttpResponseMessage releaseResponse = await _httpClient.GetAsync(
            LatestReleaseUrl,
            HttpCompletionOption.ResponseHeadersRead,
            timeout.Token);
        releaseResponse.EnsureSuccessStatusCode();

        await using Stream releaseStream = await releaseResponse.Content.ReadAsStreamAsync(timeout.Token);
        GitHubRelease? release = await JsonSerializer.DeserializeAsync<GitHubRelease>(
            releaseStream,
            JsonOptions,
            timeout.Token);

        if (release is null || !TryParseTagVersion(release.TagName, out Version latestVersion))
            throw new InvalidDataException("GitHub Release 的版本信息无效。");

        if (NormalizeVersion(latestVersion) <= NormalizeVersion(currentVersion))
            return null;

        GitHubAsset? installer = release.Assets.FirstOrDefault(asset =>
            asset.Name.Equals(InstallerAssetName, StringComparison.OrdinalIgnoreCase));
        if (installer is null || !Uri.TryCreate(installer.DownloadUrl, UriKind.Absolute, out Uri? installerUri))
            throw new InvalidDataException("GitHub Release 中没有可用的 Setup 安装程序。");

        string tagForUrl = Uri.EscapeDataString(release.TagName);
        string notesUrl =
            $"https://raw.githubusercontent.com/lyufeng000/PictureTransformer/{tagForUrl}/update.md";
        string releaseNotes = await _httpClient.GetStringAsync(notesUrl, timeout.Token);
        if (string.IsNullOrWhiteSpace(releaseNotes))
            throw new InvalidDataException("update.md 没有更新内容。");

        return new UpdateInfo(
            NormalizeVersion(latestVersion),
            release.TagName,
            releaseNotes.Trim(),
            installerUri,
            installer.Digest);
    }

    public async Task<string> DownloadInstallerAsync(
        UpdateInfo update,
        string? downloadDirectory = null,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(update);

        string destinationDirectory = string.IsNullOrWhiteSpace(downloadDirectory)
            ? KnownFolders.Downloads
            : Path.GetFullPath(downloadDirectory);
        Directory.CreateDirectory(destinationDirectory);

        string safeTag = SanitizeFileName(update.TagName);
        string destinationPath = CreateAvailablePath(
            destinationDirectory,
            $"PictureTransformer-Setup-{safeTag}",
            ".exe");
        string partialPath = destinationPath + $".{Guid.NewGuid():N}.download";

        try
        {
            using HttpResponseMessage response = await _httpClient.GetAsync(
                update.InstallerUri,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);
            response.EnsureSuccessStatusCode();

            long? contentLength = response.Content.Headers.ContentLength;
            await using Stream source = await response.Content.ReadAsStreamAsync(cancellationToken);
            await using var destination = new FileStream(
                partialPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                1024 * 128,
                FileOptions.Asynchronous | FileOptions.SequentialScan);

            byte[] buffer = ArrayPool<byte>.Shared.Rent(1024 * 128);
            try
            {
                long totalBytes = 0;
                int bytesRead;
                while ((bytesRead = await source.ReadAsync(buffer, cancellationToken)) > 0)
                {
                    await destination.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
                    totalBytes += bytesRead;
                    if (contentLength is > 0)
                        progress?.Report(totalBytes * 100d / contentLength.Value);
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }

            await destination.FlushAsync(cancellationToken);
            destination.Close();

            await VerifyDigestAsync(partialPath, update.InstallerDigest, cancellationToken);
            File.Move(partialPath, destinationPath);
            progress?.Report(100);
            return destinationPath;
        }
        catch
        {
            if (File.Exists(partialPath))
                File.Delete(partialPath);
            throw;
        }
    }

    private static HttpClient CreateHttpClient()
    {
        var client = new HttpClient { Timeout = Timeout.InfiniteTimeSpan };
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("PictureTransformer", "1.0"));
        client.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        client.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");
        return client;
    }

    private static bool TryParseTagVersion(string tagName, out Version version)
    {
        version = new Version(0, 0, 0, 0);
        if (string.IsNullOrWhiteSpace(tagName))
            return false;

        string normalized = tagName.Trim().TrimStart('v', 'V');
        int suffixIndex = normalized.IndexOfAny(['-', '+']);
        if (suffixIndex >= 0)
            normalized = normalized[..suffixIndex];

        if (!Version.TryParse(normalized, out Version? parsed))
            return false;

        version = NormalizeVersion(parsed);
        return true;
    }

    private static Version NormalizeVersion(Version version) => new(
        version.Major,
        Math.Max(version.Minor, 0),
        Math.Max(version.Build, 0),
        Math.Max(version.Revision, 0));

    private static async Task VerifyDigestAsync(
        string path,
        string? digest,
        CancellationToken cancellationToken)
    {
        const string prefix = "sha256:";
        if (string.IsNullOrWhiteSpace(digest) || !digest.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return;

        string expected = digest[prefix.Length..].Trim();
        await using var stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            1024 * 128,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        byte[] hash = await SHA256.HashDataAsync(stream, cancellationToken);
        string actual = Convert.ToHexStringLower(hash);

        if (!actual.Equals(expected, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("下载的安装程序 SHA256 校验失败。");
    }

    private static string CreateAvailablePath(string directory, string baseName, string extension)
    {
        string candidate = Path.Combine(directory, baseName + extension);
        int suffix = 2;
        while (File.Exists(candidate))
        {
            candidate = Path.Combine(directory, $"{baseName}_{suffix}{extension}");
            suffix++;
        }

        return candidate;
    }

    private static string SanitizeFileName(string value)
    {
        char[] invalidCharacters = Path.GetInvalidFileNameChars();
        return string.Concat(value.Select(character =>
            invalidCharacters.Contains(character) ? '_' : character));
    }

    private sealed class GitHubRelease
    {
        [JsonPropertyName("tag_name")]
        public string TagName { get; init; } = string.Empty;

        [JsonPropertyName("assets")]
        public IReadOnlyList<GitHubAsset> Assets { get; init; } = [];
    }

    private sealed class GitHubAsset
    {
        [JsonPropertyName("name")]
        public string Name { get; init; } = string.Empty;

        [JsonPropertyName("browser_download_url")]
        public string DownloadUrl { get; init; } = string.Empty;

        [JsonPropertyName("digest")]
        public string? Digest { get; init; }
    }
}
