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
    
        public ThemeSongsManager(ILibraryManager libraryManager, ILogger<ThemeSongsManager> logger)
        {
            _libraryManager = libraryManager;
            _logger = logger;
            
            // Log the custom path at startup to verify it's being loaded
            var customPath = Plugin.Instance.Configuration.CustomThemeSongsPath;
            _logger.LogInformation("ThemeSongs plugin v1.1.1.0 initialized with custom path: {CustomPath}", 
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
                        // Replace WebClient with HttpClient
                        var response = await _httpClient.GetAsync(link);
                        response.EnsureSuccessStatusCode();
            
                        // Download the content to a file
                        using (var fileStream = File.Create(themeSongPath))
                        {
                            await response.Content.CopyToAsync(fileStream);
                        }
            
                        _logger.LogInformation("{seriesName} theme song succesfully downloaded", serie.Name);
                    }
                    catch (Exception e)
                    {
                        _logger.LogInformation(e, "{seriesName} theme song not in database, or no internet connection", serie.Name);
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
                
                // If we found a theme song or we're using the custom path exclusively, return result
                if (themeSong != null || config.UseCustomPathExclusively)
                {
                    return themeSong;
                }
            }
            
            // Fallback to default behavior (looking in series folders) if allowed
            return FindThemeSongInDefaultLocations(item);
        }
        
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
