using System.Collections.Specialized;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Web;
using Jellyfin.Plugin.MetaTube.Metadata;

namespace Jellyfin.Plugin.MetaTube;

public static class ApiClient
{
    private const string ActorInfoApi = "/v1/actors";
    private const string MovieInfoApi = "/v1/movies";
    private const string ActorSearchApi = "/v1/actors/search";
    private const string MovieSearchApi = "/v1/movies/search";
    private const string PrimaryImageApi = "/v1/images/primary";
    private const string ThumbImageApi = "/v1/images/thumb";
    private const string BackdropImageApi = "/v1/images/backdrop";
    private const string TranslateApi = "/v1/translate";

    private static string[] GetServers()
    {
        return Plugin.Instance.Configuration.Servers?
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToArray() ?? Array.Empty<string>();
    }

    private static string GetPrimaryServer()
    {
        var servers = GetServers();
        return servers.Length > 0 ? servers[0] : string.Empty;
    }

    private static string ComposeUrl(string server, string path, NameValueCollection nv)
    {
        var query = HttpUtility.ParseQueryString(string.Empty);
        foreach (string key in nv) query.Add(key, nv.Get(key));

        // Build URL
        var uriBuilder = new UriBuilder(server)
        {
            Path = path,
            Query = query.ToString() ?? string.Empty
        };
        return uriBuilder.ToString();
    }

    private static string ComposeImageApiUrl(string path, string provider, string id, string url = default,
        double ratio = -1, double position = -1, bool auto = false, string badge = default)
    {
        return ComposeUrl(GetPrimaryServer(), $"{path.TrimEnd('/')}/{provider}/{id}", new NameValueCollection
        {
            { "url", url },
            { "ratio", ratio.ToString("R") },
            { "pos", position.ToString("R") },
            { "auto", auto.ToString() },
            { "badge", badge },
            { "quality", Plugin.Instance.Configuration.DefaultImageQuality.ToString() }
        });
    }

    private static string ComposeInfoApiUrl(string server, string path, string provider, string id, bool lazy)
    {
        return ComposeUrl(server, $"{path.TrimEnd('/')}/{provider}/{id}", new NameValueCollection
        {
            { "lazy", lazy.ToString() }
        });
    }

    private static string ComposeSearchApiUrl(string server, string path, string q, string provider, bool fallback)
    {
        return ComposeUrl(server, path, new NameValueCollection
        {
            { "q", q },
            { "provider", provider },
            { "fallback", fallback.ToString() }
        });
    }

    private static string ComposeTranslateApiUrl(string server, string path, string q, string from, string to,
        string engine, NameValueCollection nv = null)
    {
        return ComposeUrl(server, path, new NameValueCollection
        {
            { "q", q },
            { "from", from },
            { "to", to },
            { "engine", engine },
            nv ?? new NameValueCollection()
        });
    }

    public static string GetPrimaryImageApiUrl(string provider, string id, double position = -1,
        string badge = default)
    {
        return ComposeImageApiUrl(PrimaryImageApi, provider, id,
            ratio: Plugin.Instance.Configuration.PrimaryImageRatio, position: position, badge: badge);
    }

    public static string GetPrimaryImageApiUrl(string provider, string id, string url, double position = -1,
        bool auto = false, string badge = default)
    {
        return ComposeImageApiUrl(PrimaryImageApi, provider, id, url,
            Plugin.Instance.Configuration.PrimaryImageRatio, position, auto, badge);
    }

    public static string GetThumbImageApiUrl(string provider, string id)
    {
        return ComposeImageApiUrl(ThumbImageApi, provider, id);
    }

    public static string GetThumbImageApiUrl(string provider, string id, string url, double position = -1,
        bool auto = false)
    {
        return ComposeImageApiUrl(ThumbImageApi, provider, id, url, position: position, auto: auto);
    }

    public static string GetBackdropImageApiUrl(string provider, string id)
    {
        return ComposeImageApiUrl(BackdropImageApi, provider, id);
    }

    public static string GetBackdropImageApiUrl(string provider, string id, string url, double position = -1,
        bool auto = false)
    {
        return ComposeImageApiUrl(BackdropImageApi, provider, id, url, position: position, auto: auto);
    }

    public static async Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
    {
        url = NormalizeImageUrl(url);
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("User-Agent", DefaultUserAgent);
        request.Headers.Add("Accept", "image/*");

        return await HttpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }

    private static string NormalizeImageUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return url;

        var query = HttpUtility.ParseQueryString(uri.Query);
        var innerUrl = query["url"];
        if (string.IsNullOrWhiteSpace(innerUrl))
            return url;

        // Rebuild query so the nested url is safely encoded after any intermediate decoding.
        var builder = new UriBuilder(uri)
        {
            Query = query.ToString() ?? string.Empty
        };
        return builder.ToString();
    }

    public static string ReplaceImageBaseUrl(string url, string publicBaseUrl)
    {
        if (string.IsNullOrWhiteSpace(publicBaseUrl))
            return url;

        if (!Uri.TryCreate(url, UriKind.Absolute, out var originalUri))
            return url;

        if (!Uri.TryCreate(publicBaseUrl, UriKind.Absolute, out var publicBaseUri))
            return url;

        var newPath = publicBaseUri.AbsolutePath.TrimEnd('/') + originalUri.AbsolutePath;
        var builder = new UriBuilder(originalUri)
        {
            Scheme = publicBaseUri.Scheme,
            Host = publicBaseUri.Host,
            Port = publicBaseUri.IsDefaultPort ? -1 : publicBaseUri.Port,
            Path = newPath
        };
        return builder.ToString();
    }

    public static async Task<ActorInfo> GetActorInfoAsync(string provider, string id,
        CancellationToken cancellationToken)
    {
        return await GetActorInfoAsync(provider, id, true /* default */, cancellationToken);
    }

    public static async Task<ActorInfo> GetActorInfoAsync(string provider, string id, bool lazy,
        CancellationToken cancellationToken)
    {
        return await GetFirstSuccessAsync<ActorInfo>(
            server => ComposeInfoApiUrl(server, ActorInfoApi, provider, id, lazy),
            true, cancellationToken);
    }

    public static async Task<MovieInfo> GetMovieInfoAsync(string provider, string id,
        CancellationToken cancellationToken)
    {
        return await GetMovieInfoAsync(provider, id, true /* default */, cancellationToken);
    }

    public static async Task<MovieInfo> GetMovieInfoAsync(string provider, string id, bool lazy,
        CancellationToken cancellationToken)
    {
        return await GetFirstSuccessAsync<MovieInfo>(
            server => ComposeInfoApiUrl(server, MovieInfoApi, provider, id, lazy),
            true, cancellationToken);
    }

    public static async Task<List<ActorSearchResult>> SearchActorAsync(string q,
        CancellationToken cancellationToken)
    {
        return await SearchActorAsync(q, string.Empty, cancellationToken);
    }

    public static async Task<List<ActorSearchResult>> SearchActorAsync(string q, string provider,
        CancellationToken cancellationToken)
    {
        return await SearchActorAsync(q, provider, true /* default */, cancellationToken);
    }

    public static async Task<List<ActorSearchResult>> SearchActorAsync(string q, string provider,
        bool fallback, CancellationToken cancellationToken)
    {
        return await SearchAndMergeAsync<ActorSearchResult>(
            server => ComposeSearchApiUrl(server, ActorSearchApi, q, provider, fallback),
            r => (r.Provider, r.Id),
            true, cancellationToken);
    }

    public static async Task<List<MovieSearchResult>> SearchMovieAsync(string q,
        CancellationToken cancellationToken)
    {
        return await SearchMovieAsync(q, string.Empty, cancellationToken);
    }

    public static async Task<List<MovieSearchResult>> SearchMovieAsync(string q, string provider,
        CancellationToken cancellationToken)
    {
        return await SearchMovieAsync(q, provider, true /* default */, cancellationToken);
    }

    public static async Task<List<MovieSearchResult>> SearchMovieAsync(string q, string provider,
        bool fallback, CancellationToken cancellationToken)
    {
        return await SearchAndMergeAsync<MovieSearchResult>(
            server => ComposeSearchApiUrl(server, MovieSearchApi, q, provider, fallback),
            r => (r.Provider, r.Id),
            true, cancellationToken);
    }

    public static async Task<TranslationInfo> TranslateAsync(string q, string from, string to, string engine,
        NameValueCollection nv, CancellationToken cancellationToken)
    {
        return await GetFirstSuccessAsync<TranslationInfo>(
            server => ComposeTranslateApiUrl(server, TranslateApi, q, from, to, engine, nv),
            false, cancellationToken);
    }

    /// <summary>
    /// Query all servers in parallel for search operations, merge and deduplicate results.
    /// Only fails when all servers fail.
    /// </summary>
    private static async Task<List<T>> SearchAndMergeAsync<T>(
        Func<string, string> urlComposer,
        Func<T, (string, string)> dedupeKeySelector,
        bool requireAuth,
        CancellationToken cancellationToken)
    {
        var servers = GetServers();
        if (servers.Length == 0)
            throw new Exception("No MetaTube servers configured");

        if (servers.Length == 1)
        {
            var url = urlComposer(servers[0]);
            return await GetDataAsync<List<T>>(url, requireAuth, cancellationToken);
        }

        // Query all servers in parallel.
        var tasks = servers.Select(async server =>
        {
            try
            {
                var url = urlComposer(server);
                return await GetDataAsync<List<T>>(url, requireAuth, cancellationToken);
            }
            catch
            {
                return null;
            }
        }).ToList();

        var results = await Task.WhenAll(tasks);

        var successfulResults = results.Where(r => r != null).ToList();
        if (!successfulResults.Any())
            throw new Exception("All MetaTube servers failed to respond");

        // Merge and deduplicate results.
        var seen = new HashSet<(string, string)>();
        var merged = new List<T>();
        foreach (var resultList in successfulResults)
        {
            foreach (var item in resultList)
            {
                var key = dedupeKeySelector(item);
                if (seen.Add(key))
                    merged.Add(item);
            }
        }

        return merged;
    }

    /// <summary>
    /// Query all servers in parallel for single-result operations.
    /// Returns the first successful response. Only fails when all servers fail.
    /// </summary>
    private static async Task<T> GetFirstSuccessAsync<T>(
        Func<string, string> urlComposer,
        bool requireAuth,
        CancellationToken cancellationToken)
    {
        var servers = GetServers();
        if (servers.Length == 0)
            throw new Exception("No MetaTube servers configured");

        if (servers.Length == 1)
        {
            var url = urlComposer(servers[0]);
            return await GetDataAsync<T>(url, requireAuth, cancellationToken);
        }

        // Query all servers in parallel.
        var tasks = servers.Select(async server =>
        {
            var url = urlComposer(server);
            return await GetDataAsync<T>(url, requireAuth, cancellationToken);
        }).ToList();

        // Return the first successful result.
        var exceptions = new List<Exception>();
        while (tasks.Any())
        {
            var completed = await Task.WhenAny(tasks);
            tasks.Remove(completed);

            try
            {
                return await completed;
            }
            catch (Exception e)
            {
                exceptions.Add(e);
            }
        }

        throw new AggregateException("All MetaTube servers failed to respond", exceptions);
    }

    private static async Task<T> GetDataAsync<T>(string url, bool requireAuth,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var request = new HttpRequestMessage(HttpMethod.Get, url);

        // Add General Headers.
        request.Headers.Add("Accept", "application/json");
        request.Headers.Add("User-Agent", DefaultUserAgent);

        // Set API Authorization Token.
        if (requireAuth && !string.IsNullOrWhiteSpace(Plugin.Instance.Configuration.Token))
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", Plugin.Instance.Configuration.Token);

        var response = await HttpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

        // Nullable forgiving reason:
        // Response is unlikely to be null.
        // If it happens to be null, an exception is planed to be thrown either way.
        var apiResponse = (await response.Content!
            .ReadFromJsonAsync<ResponseInfo<T>>(cancellationToken: cancellationToken).ConfigureAwait(false))!;

        // EnsureSuccessStatusCode ignoring reason:
        // When the status is unsuccessful, the API response contains error details.
        if (!response.IsSuccessStatusCode && apiResponse.Error != null)
            throw new Exception($"API request error: {apiResponse.Error.Code} ({apiResponse.Error.Message})");

        // Note: data field must not be null if there are no errors.
        if (apiResponse.Data == null)
            throw new Exception("Response data field is null");

        return apiResponse.Data;
    }

    #region Http

    private static readonly HttpClient HttpClient;
    private static string DefaultUserAgent => $"{Plugin.ProviderName}/{Plugin.Instance.Version}";

    static ApiClient()
    {
        HttpClient = new HttpClient(new SocketsHttpHandler
        {
            // Connect Timeout.
            ConnectTimeout = TimeSpan.FromSeconds(30),

            // TCP Keep Alive.
            KeepAlivePingPolicy = HttpKeepAlivePingPolicy.Always,
            KeepAlivePingDelay = TimeSpan.FromSeconds(30),
            KeepAlivePingTimeout = TimeSpan.FromSeconds(30),

            // Connection Pooling.
            PooledConnectionLifetime = TimeSpan.FromMinutes(10),
            PooledConnectionIdleTimeout = TimeSpan.FromSeconds(90)
        });
    }

    #endregion
}
