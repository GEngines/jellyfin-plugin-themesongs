using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.ThemeSongs.Configuration
{
    public class PluginConfiguration : BasePluginConfiguration
    {
        /// <summary>
        /// Gets or sets the custom central path for all theme songs.
        /// </summary>
        public string CustomThemeSongsPath { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets a value indicating whether to use custom path exclusively.
        /// </summary>
        public bool UseCustomPathExclusively { get; set; } = false;
        
        public PluginConfiguration()
        {
            // Initialize default values
            CustomThemeSongsPath = string.Empty;
            UseCustomPathExclusively = false;
            
        }
    }
}