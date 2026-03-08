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

    public Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
    {
        Logger.Debug("GetImageResponse for url: {0}", url);
        return ApiClient.GetImageResponse(url, cancellationToken);
    }
}