using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;

namespace Jellyfin.Plugin.MetaTube.ExternalIds;

public class TrailerExternalId : BaseExternalId
{
    public override string ProviderName => "TrailerUrl";

    public override ExternalIdMediaType? Type => ExternalIdMediaType.Movie;

    public override string Key => "TrailerUrl";

    public override string UrlFormatString => null;

    public override bool Supports(IHasProviderIds item)
    {
        return item is Movie;
    }
}