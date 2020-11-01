using System;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Services;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.ThemeSongs.Api
{
    [Route("/ThemeSongs/Download", "POST", Summary = "Downloads theme songs")]
    [Authenticated]
    public class DownloadRequest : IReturnVoid
    {
    }

    public class ThemeSongsService : IService
    {
        private readonly ThemeSongsManager _themeSongsManager;
        private readonly ILogger<ThemeSongsService> _logger;

        public ThemeSongsService(
            ILibraryManager libraryManager,
            ILogger<ThemeSongsService> logger)
        {
            _themeSongsManager = new ThemeSongsManager(libraryManager,  logger);
            _logger = logger;
        }
        
        public void Post(DownloadRequest request)
        {
            _logger.LogInformation("Starting a manual refresh, looking up for repeated versions");
            _themeSongsManager.DownloadAllTVThemeSongs();
            _logger.LogInformation("Completed refresh");
        }

        

    }
}