using System.Net.Mime;
using MediaBrowser.Controller.Library;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Jellyfin.Plugin.ThemeSongs.Configuration;

namespace Jellyfin.Plugin.ThemeSongs.Api
{
    /// <summary>
    /// The Theme Songs api controller.
    /// </summary>
    [ApiController]
    [Route("ThemeSongs")]
    [Produces(MediaTypeNames.Application.Json)]
    

    public class ThemeSongsController : ControllerBase
    {
        private readonly ThemeSongsManager _themeSongsManager;
        private readonly ILogger<ThemeSongsManager> _logger;

        /// <summary>
        /// Initializes a new instance of <see cref="ThemeSongsController"/>.

        public ThemeSongsController(
            ILibraryManager libraryManager,
            ILogger<ThemeSongsManager> logger)
        {
            _themeSongsManager = new ThemeSongsManager(libraryManager,  logger);
            _logger = logger;
        }

        /// <summary>
        /// Downloads all Tv theme songs.
        /// </summary>
        /// <reponse code="204">Theme song download started successfully. </response>
        /// <returns>A <see cref="NoContentResult"/> indicating success.</returns>
        [HttpPost("DownloadTVShows")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<ActionResult> DownloadTVShows()
        {
            _logger.LogInformation("Downloading TV Theme Songs");
            await _themeSongsManager.DownloadAllThemeSongs();
            _logger.LogInformation("Completed");
            return NoContent();
        }

        /// <summary>
        /// Updates the plugin configuration.
        /// </summary>
        /// <param name="configuration">The new configuration settings.</param>
        /// <returns>A <see cref="NoContentResult"/> indicating success.</returns>
        [HttpPost("Configuration")]
        [Authorize(Policy = "RequiresElevation")]
        [Consumes(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public ActionResult UpdateConfiguration([FromBody] PluginConfiguration configuration)
        {
            try
            {
                // Update the plugin configuration
                Plugin.Instance.Configuration.CustomThemeSongsPath = configuration.CustomThemeSongsPath;
                Plugin.Instance.Configuration.UseCustomPathExclusively = configuration.UseCustomPathExclusively;
                
                // Save configuration to disk
                Plugin.Instance.SaveConfiguration();
                
                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest($"Error saving configuration: {ex.Message}");
            }
        }
    }
}