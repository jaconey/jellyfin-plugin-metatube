using Jellyfin.Plugin.MetaTube.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Common.Configuration;

namespace Jellyfin.Plugin.MetaTube;

public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
{
    public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer) : base(applicationPaths,
        xmlSerializer)
    {
        Instance = this;
    }

    public const string ProviderName = "MetaTube";

    public const string ProviderId = "MetaTube";

    public override string Name => ProviderName;

    public override string Description => "MetaTube Plugin for Jellyfin";

    public override Guid Id => Guid.Parse("01cc53ec-c415-4108-bbd4-a684a9801a32");

    public static Plugin Instance { get; private set; }

    public IEnumerable<PluginPageInfo> GetPages()
    {
        return new[]
        {
            new PluginPageInfo
            {
                Name = Name,
                EmbeddedResourcePath = $"{GetType().Namespace}.Configuration.configPage.html"
            }
        };
    }
}