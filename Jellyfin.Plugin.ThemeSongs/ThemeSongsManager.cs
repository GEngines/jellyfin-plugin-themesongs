using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;
using Jellyfin.Data.Enums;

namespace Jellyfin.Plugin.ThemeSongs
{
    public class ThemeSongsManager : IDisposable
    {
        private readonly ILibraryManager _libraryManager;
        private readonly Timer _timer;
        private readonly ILogger<ThemeSongsManager> _logger;

        // Add a static HttpClient instance to the class (better for performance)
        private static readonly HttpClient _httpClient = new HttpClient();
        private static readonly SemaphoreSlim _downloadThrottler = new SemaphoreSlim(3, 3); // Max 3 concurrent downloads
    
        public ThemeSongsManager(ILibraryManager libraryManager, ILogger<ThemeSongsManager> logger)
        {
            _libraryManager = libraryManager;
            _logger = logger;
            
            // Check for environment variable override first
            string envPath = Environment.GetEnvironmentVariable("JELLYFIN_THEME_SONGS_PATH");
            if (!string.IsNullOrWhiteSpace(envPath))
            {
                // Environment variable takes precedence if set
                Plugin.Instance.Configuration.CustomThemeSongsPath = envPath;
                Plugin.Instance.SaveConfiguration();
                _logger.LogInformation("Theme songs path set from environment variable: {Path}", envPath);
            }
            
            // Log the custom path at startup to verify it's being loaded
            var customPath = Plugin.Instance.Configuration.CustomThemeSongsPath;
            _logger.LogInformation("ThemeSongs plugin v1.1.2.0 initialized with custom path: {CustomPath}", 
                string.IsNullOrEmpty(customPath) ? "(not set)" : customPath);
            
            _timer = new Timer(_ => OnTimerElapsed(), null, Timeout.Infinite, Timeout.Infinite);
        }

        private IEnumerable<Series> GetSeriesFromLibrary()
        {
            return _libraryManager.GetItemList(new InternalItemsQuery
            {
                IncludeItemTypes = new[] {BaseItemKind.Series},
                IsVirtualItem = false,
                Recursive = true,
                HasTvdbId = true
            }).Select(m => m as Series);
        }


        // EnsureCustomDirectoryExists method already exists elsewhere in the file

        public async Task DownloadAllThemeSongs()
        {
            var series = GetSeriesFromLibrary();
            foreach (var serie in series)
            {
                if (serie.GetThemeSongs().Count() == 0)
                {
                    var tvdb = serie.GetProviderId(MetadataProvider.Tvdb);
                    var config = Plugin.Instance.Configuration;
                    string themeSongPath;
                    
                    // Use custom path if configured and available
                    if (!string.IsNullOrWhiteSpace(config.CustomThemeSongsPath) && EnsureCustomDirectoryExists())
                    {
                        // Use TVDB ID for uniqueness
                        themeSongPath = Path.Combine(config.CustomThemeSongsPath, $"{tvdb}.mp3");
                    }
                    else
                    {
                        // Fallback to series path
                        themeSongPath = Path.Join(serie.Path, "theme.mp3");
                    }

                    var link = $"http://tvthemes.plexapp.com/{tvdb}.mp3";
                    _logger.LogInformation("Trying to download {seriesName}, {link}", serie.Name, link);

                    try
                    {
                        // Throttle downloads to prevent overwhelming the server
                        await _downloadThrottler.WaitAsync();
                        try
                        {
                            // Use HttpClient with a timeout
                            var response = await _httpClient.GetAsync(link);
                            
                            // Check status code without throwing exception
                            if (response.IsSuccessStatusCode)
                            {
                                // Download the content to a file
                                using (var fileStream = File.Create(themeSongPath))
                                {
                                    await response.Content.CopyToAsync(fileStream);
                                }
                                
                                _logger.LogInformation("Theme song for '{SeriesName}' successfully downloaded", serie.Name);
                                
                                // Register the theme song with our mapping system
                                RegisterThemeSongWithJellyfin(serie, themeSongPath);
                            }
                            else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                            {
                                _logger.LogInformation("No theme song available for '{SeriesName}' (TVDB ID: {TvdbId})", 
                                    serie.Name, tvdb);
                            }
                            else
                            {
                                _logger.LogWarning("Failed to download theme song for '{SeriesName}': HTTP {StatusCode}", 
                                    serie.Name, (int)response.StatusCode);
                            }
                        }
                        catch (HttpRequestException ex)
                        {
                            _logger.LogWarning("Connection error downloading theme for '{SeriesName}': {Message}", 
                                serie.Name, ex.Message);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Unexpected error downloading theme for '{SeriesName}'", serie.Name);
                        }
                        finally
                        {
                            // Always release the semaphore
                            _downloadThrottler.Release();
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Fatal error processing series '{SeriesName}'", serie.Name);
                    }
                }
            }        
        }


        private void OnTimerElapsed()
        {
            // Stop the timer until next update
            _timer.Change(Timeout.Infinite, Timeout.Infinite);
        }

        public Task RunAsync()
        {
            return Task.CompletedTask;
        }

        public void Dispose()
        {
        }

        public string GetThemeSong(BaseItem item)
        {
            var config = Plugin.Instance.Configuration;
            string themeSong = null;
            
            // Check custom path first if configured
            if (!string.IsNullOrWhiteSpace(config.CustomThemeSongsPath))
            {
                themeSong = FindThemeSongInCustomPath(item);
                
                // If we found a theme song, register it with Jellyfin and return result
                if (themeSong != null)
                {
                    RegisterThemeSongWithJellyfin(item, themeSong);
                    return themeSong;
                }
                
                // If using custom path exclusively, don't check default locations
                if (config.UseCustomPathExclusively)
                {
                    return null;
                }
            }
            
            // Fallback to default behavior (looking in series folders) if allowed
            return FindThemeSongInDefaultLocations(item);
        }
        
        /// <summary>
        /// Registers a theme song with Jellyfin's library system so it will be recognized
        /// </summary>
        private void RegisterThemeSongWithJellyfin(BaseItem item, string themeSongPath)
        {
            try
            {
                if (item is Series series)
                {
                    _logger.LogDebug("Registering theme song for {Name}: {Path}", item.Name, themeSongPath);
                    
                    // Always use direct path mapping approach
                    _logger.LogInformation("Using theme song from custom path: {Path}", themeSongPath);
                    
                    // Store the path mapping in plugin configuration
                    var config = Plugin.Instance.Configuration;
                    
                    // Check if we already have a mapping for this item
                    string itemId = item.Id.ToString();
                    var existingMapping = config.ThemeMappings.FirstOrDefault(m => m.ItemId == itemId);
                    
                    if (existingMapping != null)
                    {
                        // Update existing mapping
                        existingMapping.ThemePath = themeSongPath;
                    }
                    else
                    {
                        // Add new mapping
                        config.ThemeMappings.Add(new Configuration.ThemePathMapping(itemId, themeSongPath));
                    }
                    
                    Plugin.Instance.SavePluginConfiguration();
                    
                    _logger.LogDebug("Stored theme path mapping for {Name}", series.Name);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering theme song: {Path}", themeSongPath);
            }
        }
        
        // No longer needed with direct mapping approach
        
        private string FindThemeSongInCustomPath(BaseItem item)
        {
            try
            {
                var config = Plugin.Instance.Configuration;
                var customPath = config.CustomThemeSongsPath;
                
                if (string.IsNullOrWhiteSpace(customPath) || !Directory.Exists(customPath))
                {
                    return null;
                }
                
                // Try to find theme song with various naming patterns
                var potentialFileNames = new List<string>
                {
                    // By series name
                    $"{item.Name}.mp3",
                    $"{item.Name} theme.mp3",
                    $"{item.Name}_theme.mp3",
                    
                    // By ID for unique identification
                    $"{item.Id}.mp3",
                    
                    // Support other audio formats
                    $"{item.Name}.m4a",
                    $"{item.Name}.ogg",
                    $"{item.Name}.wav",
                    $"{item.Id}.m4a",
                    $"{item.Id}.ogg",
                    $"{item.Id}.wav"
                };
                
                foreach (var fileName in potentialFileNames)
                {
                    var sanitizedFileName = Path.GetInvalidFileNameChars()
                        .Aggregate(fileName, (current, c) => current.Replace(c, '_'));
                        
                    var fullPath = Path.Combine(customPath, sanitizedFileName);
                    if (File.Exists(fullPath))
                    {
                        _logger.LogInformation("Found theme song at custom path: {Path}", fullPath);
                        return fullPath;
                    }
                }
                
                // Also try a subdirectory with the series name
                var seriesFolder = Path.Combine(customPath, item.Name);
                if (Directory.Exists(seriesFolder))
                {
                    foreach (var ext in new[] { "*.mp3", "*.m4a", "*.ogg", "*.wav" })
                    {
                        var files = Directory.GetFiles(seriesFolder, ext);
                        if (files.Length > 0)
                        {
                            _logger.LogInformation("Found theme song in series subfolder: {Path}", files[0]);
                            return files[0];
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding theme song in custom path for {Name}", item.Name);
            }
            
            return null;
        }
        
        private string FindThemeSongInDefaultLocations(BaseItem item)
        {
            // Check if we have a mapping for this item in our configuration
            // This is the case for read-only media directories
            var config = Plugin.Instance.Configuration;
            string itemId = item.Id.ToString();
            
            // Use the ThemePathMappingsCache for faster lookup
            var mappings = config.ThemePathMappingsCache;
            if (mappings.TryGetValue(itemId, out string mappedPath))
            {
                if (File.Exists(mappedPath))
                {
                    _logger.LogInformation("Using mapped theme song path for {Name}: {Path}", item.Name, mappedPath);
                    return mappedPath;
                }
                else
                {
                    // Remove invalid mapping
                    var mappingToRemove = config.ThemeMappings.FirstOrDefault(m => m.ItemId == itemId);
                    if (mappingToRemove != null)
                    {
                        config.ThemeMappings.Remove(mappingToRemove);
                        Plugin.Instance.SavePluginConfiguration();
                    }
                }
            }
            
            // Standard approach - check series folder
            var series = item as Series;
            if (series == null)
            {
                return null;
            }

            // Check in the series folder
            var seriesFolder = series.Path;
            if (Directory.Exists(seriesFolder))
            {
                foreach (var ext in new[] { "*.mp3", "*.m4a", "*.ogg", "*.wav" })
                {
                    var files = Directory.GetFiles(seriesFolder, ext);
                    if (files.Length > 0)
                    {
                        _logger.LogInformation("Found theme song in series folder: {Path}", files[0]);
                        return files[0];
                    }
                }
            }

            return null;
        }

        // Add a method to ensure the custom directory exists
        private bool EnsureCustomDirectoryExists()
        {
            var config = Plugin.Instance.Configuration;
            
            try
            {
                if (!Directory.Exists(config.CustomThemeSongsPath))
                {
                    Directory.CreateDirectory(config.CustomThemeSongsPath);
                    _logger.LogInformation("Created custom theme songs directory: {Path}", 
                        config.CustomThemeSongsPath);
                }
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create custom theme songs directory: {Path}", 
                    config.CustomThemeSongsPath);
                return false;
            }
        }
    }
}
