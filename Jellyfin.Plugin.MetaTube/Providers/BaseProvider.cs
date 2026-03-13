using Jellyfin.Plugin.MetaTube.Configuration;
using Jellyfin.Plugin.MetaTube.Extensions;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.MetaTube.Providers;

public abstract class BaseProvider
{
    protected readonly ILogger Logger;

    protected BaseProvider(ILogger logger)
    {
        Logger = logger;
    }

    protected static PluginConfiguration Configuration => Plugin.Instance.Configuration;

    public virtual int Order => 1;

    public virtual string Name => Plugin.ProviderName;

    protected string? GetPublicImageUrl(string url)
    {
        var publicBaseUrl = Configuration.PublicImageBaseUrl;
        return string.IsNullOrWhiteSpace(publicBaseUrl)
            ? null
            : ApiClient.ReplaceImageBaseUrl(url, publicBaseUrl);
    }

    public async Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
    {
        Logger.Info("GetImageResponse for url: {0}", url);
        try
        {
            var response = await ApiClient.GetImageResponse(url, cancellationToken).ConfigureAwait(false);
            Logger.Info("GetImageResponse status: {0} for url: {1}", (int)response.StatusCode, url);
            return response;
        }
        catch (Exception e)
        {
            Logger.Error("GetImageResponse failed for url: {0} ({1})", url, e.Message);
            throw;
        }
    }
}
