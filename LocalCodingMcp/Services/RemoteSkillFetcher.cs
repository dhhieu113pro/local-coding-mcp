using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;

namespace LocalCodingMcp.Services;

public sealed record RemoteSkillFetchResult(
    string SourceUrl,
    string ResolvedSourceUrl,
    string Content,
    string ContentSha256,
    string? ETag,
    DateTimeOffset? LastModified);

public sealed class RemoteSkillFetcher
{
    private readonly HttpClient _httpClient;
    private readonly int _maxBytes;
    private readonly int _maxRedirects;

    public RemoteSkillFetcher(HttpClient httpClient, int maxBytes, int maxRedirects)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        if (maxBytes <= 0) throw new ArgumentOutOfRangeException(nameof(maxBytes));
        if (maxRedirects < 0) throw new ArgumentOutOfRangeException(nameof(maxRedirects));
        _httpClient = httpClient;
        _maxBytes = maxBytes;
        _maxRedirects = maxRedirects;
    }

    public async Task<RemoteSkillFetchResult> FetchAsync(
        string sourceUrl,
        string? etag = null,
        DateTimeOffset? lastModified = null,
        CancellationToken cancellationToken = default)
    {
        var original = ValidateAndNormalize(sourceUrl, out var current);

        for (var redirect = 0; ; redirect++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, current);
            if (!string.IsNullOrWhiteSpace(etag))
            {
                request.Headers.IfNoneMatch.Add(EntityTagHeaderValue.Parse(etag));
            }
            if (lastModified.HasValue)
            {
                request.Headers.IfModifiedSince = lastModified;
            }

            using var response = await _httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            if (IsRedirect(response.StatusCode))
            {
                if (redirect >= _maxRedirects)
                {
                    throw new InvalidDataException("Remote skill source exceeded the redirect limit.");
                }

                var location = response.Headers.Location
                    ?? throw new InvalidDataException("Remote skill redirect is missing Location.");
                current = location.IsAbsoluteUri ? location : new Uri(current, location);
                ValidateHttpsUri(current);
                continue;
            }

            response.EnsureSuccessStatusCode();
            ValidateContentType(response.Content.Headers.ContentType);
            if (response.Content.Headers.ContentLength is long length && length > _maxBytes)
            {
                throw new InvalidDataException($"Remote skill exceeds the {_maxBytes}-byte limit.");
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var buffer = new MemoryStream();
            var chunk = new byte[8192];
            while (true)
            {
                var read = await stream.ReadAsync(chunk.AsMemory(0, chunk.Length), cancellationToken);
                if (read == 0) break;
                if (buffer.Length + read > _maxBytes)
                {
                    throw new InvalidDataException($"Remote skill exceeds the {_maxBytes}-byte limit.");
                }
                buffer.Write(chunk, 0, read);
            }

            var bytes = buffer.ToArray();
            if (bytes.Length == 0 || bytes.Any(b => b == 0))
            {
                throw new InvalidDataException("Remote skill content is empty or binary.");
            }

            var content = Encoding.UTF8.GetString(bytes);
            if (string.IsNullOrWhiteSpace(content))
            {
                throw new InvalidDataException("Remote skill content is empty.");
            }

            var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(content))).ToLowerInvariant();
            return new RemoteSkillFetchResult(
                original,
                current.ToString(),
                content,
                hash,
                response.Headers.ETag?.ToString(),
                response.Content.Headers.LastModified);
        }
    }

    private static string ValidateAndNormalize(string sourceUrl, out Uri resolved)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceUrl);
        if (!Uri.TryCreate(sourceUrl, UriKind.Absolute, out var source))
        {
            throw new InvalidDataException("Remote skill source must be an absolute HTTPS URL.");
        }
        ValidateHttpsUri(source);

        if (source.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase))
        {
            var segments = source.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length >= 5 && segments[2].Equals("blob", StringComparison.OrdinalIgnoreCase))
            {
                var rawPath = string.Join('/', segments.Skip(3));
                resolved = new Uri($"https://raw.githubusercontent.com/{segments[0]}/{segments[1]}/{rawPath}");
                return source.ToString();
            }
        }

        resolved = source;
        return source.ToString();
    }

    private static void ValidateHttpsUri(Uri uri)
    {
        if (!uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Remote skill sources and redirects must use HTTPS.");
        if (!string.IsNullOrEmpty(uri.UserInfo))
            throw new InvalidDataException("Remote skill URLs must not contain embedded credentials.");
    }

    private static void ValidateContentType(MediaTypeHeaderValue? contentType)
    {
        if (contentType is null) return;
        var mediaType = contentType.MediaType ?? string.Empty;
        if (mediaType.StartsWith("text/", StringComparison.OrdinalIgnoreCase)
            || mediaType.Equals("application/markdown", StringComparison.OrdinalIgnoreCase)
            || mediaType.Equals("application/x-markdown", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }
        throw new InvalidDataException($"Remote skill content type '{mediaType}' is not text/Markdown.");
    }

    private static bool IsRedirect(HttpStatusCode statusCode) => statusCode is
        HttpStatusCode.MovedPermanently or HttpStatusCode.Redirect or HttpStatusCode.RedirectMethod
        or HttpStatusCode.TemporaryRedirect or HttpStatusCode.PermanentRedirect;
}
