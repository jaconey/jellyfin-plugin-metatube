using Jellyfin.Plugin.MetaTube.Extensions;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.MetaTube.Providers;

public class MovieImageProvider : BaseProvider, IRemoteImageProvider, IHasOrder
{
    public MovieImageProvider(ILogger<MovieImageProvider> logger) : base(logger)
    {
    }

    public async Task<IEnumerable<RemoteImageInfo>> GetImages(BaseItem item, CancellationToken cancellationToken)
    {
        var pid = item.GetPid(Plugin.ProviderId);
        if (string.IsNullOrWhiteSpace(pid.Id) || string.IsNullOrWhiteSpace(pid.Provider))
            return Enumerable.Empty<RemoteImageInfo>();

        var m = await ApiClient.GetMovieInfoAsync(pid.Provider, pid.Id, cancellationToken);
        Logger.Info("Movie image source urls for item: {0} cover: {1} thumb: {2} backdrop: {3}",
            item.Name, m.CoverUrl, m.ThumbUrl, m.BigCoverUrl);
        var primaryUrl = ApiClient.GetPrimaryImageApiUrl(m.Provider, m.Id, m.CoverUrl, pid.Position ?? -1, true);
        var thumbUrl = ApiClient.GetThumbImageApiUrl(m.Provider, m.Id, m.ThumbUrl, pid.Position ?? -1, true);
        var backdropUrl = ApiClient.GetBackdropImageApiUrl(m.Provider, m.Id, m.BigCoverUrl, -1, true);

        var images = new List<RemoteImageInfo>
        {
            new()
            {
                ProviderName = Name,
                Type = ImageType.Primary,
                Url = primaryUrl,
                ThumbnailUrl = GetClientImageUrl(primaryUrl)
            },
            new()
            {
                ProviderName = Name,
                Type = ImageType.Thumb,
                Url = thumbUrl,
                ThumbnailUrl = GetClientImageUrl(thumbUrl)
            },
            new()
            {
                ProviderName = Name,
                Type = ImageType.Backdrop,
                Url = backdropUrl,
                ThumbnailUrl = GetClientImageUrl(backdropUrl)
            }
        };

        foreach (var imageUrl in m.PreviewImages ?? Enumerable.Empty<string>())
        {
            Logger.Info("Movie preview image source url: {0} for item: {1}", imageUrl, item.Name);
            var previewPrimaryUrl =
                ApiClient.GetPrimaryImageApiUrl(m.Provider, m.Id, imageUrl, pid.Position ?? -1, true);
            images.Add(new RemoteImageInfo
            {
                ProviderName = Name,
                Type = ImageType.Primary,
                Url = previewPrimaryUrl,
                ThumbnailUrl = GetClientImageUrl(previewPrimaryUrl)
            });

            var previewThumbUrl = ApiClient.GetThumbImageApiUrl(m.Provider, m.Id, imageUrl, -1, true);
            images.Add(new RemoteImageInfo
            {
                ProviderName = Name,
                Type = ImageType.Thumb,
                Url = previewThumbUrl,
                ThumbnailUrl = GetClientImageUrl(previewThumbUrl)
            });

            var previewBackdropUrl = ApiClient.GetBackdropImageApiUrl(m.Provider, m.Id, imageUrl, -1, true);
            images.Add(new RemoteImageInfo
            {
                ProviderName = Name,
                Type = ImageType.Backdrop,
                Url = previewBackdropUrl,
                ThumbnailUrl = GetClientImageUrl(previewBackdropUrl)
            });
        }

        foreach (var image in images)
        {
            Logger.Info("Movie image url: {0} (thumb: {1}) for item: {2}", image.Url, image.ThumbnailUrl, item.Name);
        }

        return images;
    }

    public bool Supports(BaseItem item)
    {
        return item is Movie;
    }

    public IEnumerable<ImageType> GetSupportedImages(BaseItem item)
    {
        return new List<ImageType>
        {
            ImageType.Primary,
            ImageType.Thumb,
            ImageType.Backdrop
        };
    }
}
