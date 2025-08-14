using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using System;
using System.IO;
using Jellyfin.Plugin.ThemeSongs.Configuration;

namespace Jellyfin.Plugin.ThemeSongs
{
    public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
    {
        // Add a reference to the configuration manager
        private readonly IApplicationPaths _applicationPaths;
        private readonly IXmlSerializer _xmlSerializer;

        public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
            : base(applicationPaths, xmlSerializer)
        {
            _applicationPaths = applicationPaths;
            _xmlSerializer = xmlSerializer;
            
            // Load saved configuration at startup
            Instance = this;
        }

        // Add or update this method to save configuration
        public override void SaveConfiguration()
        {
            string configPath = GetConfigurationFilePath();
            Directory.CreateDirectory(Path.GetDirectoryName(configPath));
            
            _xmlSerializer.SerializeToFile(Configuration, configPath);
        }

        // Override UpdateConfiguration to trigger SaveConfiguration when settings change
        public override void UpdateConfiguration(BasePluginConfiguration configuration)
        {
            base.UpdateConfiguration(configuration);
            SaveConfiguration();
        }

        // Add a public method to manually save configuration
        public void SavePluginConfiguration()
        {
            SaveConfiguration();
        }
        
        private string GetConfigurationFilePath()
        {
            return Path.Combine(_applicationPaths.ConfigurationDirectoryPath, 
                $"plugins/{Id}/{ConfigurationFileName}");
        }
        
        // Make sure this static instance property exists for access from ThemeSongsManager
        public static Plugin Instance { get; private set; }

        public override string Name => "Theme Songs";

        public override string Description
            => "Downloads Theme Songs";

        private readonly Guid _id = new Guid("afe1de9c-63e4-4692-8d8c-7c964df19eb2");
        public override Guid Id => _id;

        public System.Collections.Generic.IEnumerable<PluginPageInfo> GetPages()
        {
            return new[]
            {
                new PluginPageInfo
                {
                    Name = "Theme Songs",
                    EmbeddedResourcePath = GetType().Namespace + ".Configuration.configurationpage.html"
                }
            };
        }
    }
}
