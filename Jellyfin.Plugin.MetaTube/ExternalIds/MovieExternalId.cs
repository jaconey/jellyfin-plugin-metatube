using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;

namespace Jellyfin.Plugin.MetaTube.ExternalIds;

public class MovieExternalId : BaseExternalId
{
    public override ExternalIdMediaType? Type => ExternalIdMediaType.Movie;

    public override bool Supports(IHasProviderIds item)
    {
        return item is Movie;
    }
}