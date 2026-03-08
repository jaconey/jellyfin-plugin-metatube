using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;

namespace Jellyfin.Plugin.MetaTube.ExternalIds;

public abstract class BaseExternalId : IExternalId
{
    public virtual string ProviderName => Plugin.ProviderName;

    public abstract ExternalIdMediaType? Type { get; }

    public virtual string Key => Plugin.ProviderId;

    public virtual string UrlFormatString
    {
        get
        {
            var servers = Plugin.Instance.Configuration.Servers;
            var server = servers?.Length > 0 ? servers[0] : string.Empty;
            return server + "?redirect={0}";
        }
    }

    public abstract bool Supports(IHasProviderIds item);
}