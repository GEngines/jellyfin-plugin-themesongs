using MediaBrowser.Model.Plugins;
using System;
using System.Collections.Generic;
using System.Xml.Serialization;

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
        
        /// <summary>
        /// Gets or sets the playback volume for theme songs (0-100).
        /// </summary>
        public int ThemeVolume { get; set; } = 20;
        
        /// <summary>
        /// Gets or sets theme mappings collection for read-only media directories.
        /// </summary>
        [XmlArray("ThemePathMappings")]
        [XmlArrayItem("Mapping")]
        public List<ThemePathMapping> ThemeMappings { get; set; } = new List<ThemePathMapping>();
        
        /// <summary>
        /// Helper method to get a theme path from the mappings
        /// </summary>
        [XmlIgnore]
        public Dictionary<string, string> ThemePathMappingsCache 
        { 
            get 
            {
                // Build a dictionary from the list for faster lookups
                var dict = new Dictionary<string, string>();
                foreach (var mapping in ThemeMappings)
                {
                    dict[mapping.ItemId] = mapping.ThemePath;
                }
                return dict;
            }
        }
        
        public PluginConfiguration()
        {
            // Initialize default values
            CustomThemeSongsPath = string.Empty;
            UseCustomPathExclusively = false;
            ThemeVolume = 20; // Default to 20% volume
            ThemeMappings = new List<ThemePathMapping>();
        }
    }
    
    /// <summary>
    /// Represents a mapping between an item ID and its theme song path
    /// </summary>
    public class ThemePathMapping
    {
        /// <summary>
        /// Gets or sets the item ID
        /// </summary>
        [XmlAttribute("itemId")]
        public string ItemId { get; set; }
        
        /// <summary>
        /// Gets or sets the theme song path
        /// </summary>
        [XmlAttribute("path")]
        public string ThemePath { get; set; }
        
        public ThemePathMapping()
        {
            // Parameterless constructor required for XML serialization
        }
        
        public ThemePathMapping(string itemId, string themePath)
        {
            ItemId = itemId;
            ThemePath = themePath;
        }
    }
}