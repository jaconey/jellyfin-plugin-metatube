using Jellyfin.Plugin.MetaTube.Extensions;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.MetaTube.Providers;

public class ActorImageProvider : BaseProvider, IRemoteImageProvider, IHasOrder
{
    public ActorImageProvider(ILogger<ActorImageProvider> logger) : base(logger)
    {
    }

    public async Task<IEnumerable<RemoteImageInfo>> GetImages(BaseItem item, CancellationToken cancellationToken)
    {
        var pid = item.GetPid(Plugin.ProviderId);
        if (string.IsNullOrWhiteSpace(pid.Id) || string.IsNullOrWhiteSpace(pid.Provider))
            return Enumerable.Empty<RemoteImageInfo>();

        var actorInfo = await ApiClient.GetActorInfoAsync(pid.Provider, pid.Id, cancellationToken);

        if (actorInfo.Images?.Any() != true)
            return Enumerable.Empty<RemoteImageInfo>();

        foreach (var imageUrl in actorInfo.Images)
        {
            Logger.Info("Actor image source url: {0} for item: {1}", imageUrl, item.Name);
        }

        var images = actorInfo.Images.Select(image =>
        {
            var primaryUrl = ApiClient.GetPrimaryImageApiUrl(actorInfo.Provider, actorInfo.Id, image, 0.5, true);
            return new RemoteImageInfo
            {
                ProviderName = Name,
                Type = ImageType.Primary,
                Url = primaryUrl,
                ThumbnailUrl = GetClientImageUrl(primaryUrl)
            };
        }).ToList();

        foreach (var image in images)
        {
            Logger.Info("Actor image url: {0} (thumb: {1}) for item: {2}", image.Url, image.ThumbnailUrl,
                item.Name);
        }

        return images;
    }

    public bool Supports(BaseItem item)
    {
        return item is Person;
    }

    public IEnumerable<ImageType> GetSupportedImages(BaseItem item)
    {
        return new List<ImageType>
        {
            ImageType.Primary
        };
    }
}
