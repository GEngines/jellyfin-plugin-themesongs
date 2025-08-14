<h1 align="center">Jellyfin Theme Songs Plugin</h1>
<h3 align="center">Part of the <a href="https://jellyfin.org">Jellyfin Project</a></h3>

<p align="center">
Jellyfin Theme Songs plugin is a plugin that automatically downloads every theme song of your tv show library.
</p>

<p align="center">
This is a fork of the <a href="https://github.com/danieladov/jellyfin-plugin-themesongs">original Theme Songs plugin</a> by danieladov, with added support for custom theme songs paths.
</p>

## Install Process


## From Repository
1. In jellyfin, go to dashboard -> plugins -> Repositories -> add and paste this link:
   ```
   https://raw.githubusercontent.com/GEngines/JellyfinPluginManifest/main/manifests/advanced_theme_songs.json
   ```
2. Go to Catalog and search for Theme Songs
3. Click on it and install
4. Restart Jellyfin


## From .zip file
1. Download the .zip file from release page
2. Extract it and place the .dll file in a folder called ```plugins/Theme Songs``` under  the program data directory or inside the portable install directory
3. Restart Jellyfin

## User Guide
1. To download the theme songs you can do it from Schedule task or directly from the configuration of the plugin.
2. You need to have enabled the option "Theme Songs" under display settings.
3. Custom Theme Songs Path:
   - In the plugin settings, you can specify a custom directory to store all your theme songs.
   - Enable "Use custom path exclusively" if you only want theme songs to be found in the custom path.
   - When disabled, the plugin will fall back to searching in the TV series folders if a theme is not found in the custom path.
   
## Changing Theme Directory

There are several ways to set up a custom theme songs directory:

### Using the Plugin Settings
1. Go to the plugin configuration page in Jellyfin
2. Enter your desired path in the "Custom Theme Songs Path" field
3. Click "Save Settings" 
4. Click "Download Theme Songs" to download themes to the new location

### How the Custom Path Works

When you configure a custom themes path:

1. **Theme Storage**: All theme songs will be downloaded to and stored in your custom path
2. **Theme Detection**: 
   - When you enable "Use custom path exclusively", the plugin will only look for themes in your custom path
   - Otherwise, it will check your custom path first, then fall back to series folders
3. **Jellyfin Integration**:
   - The plugin maintains a mapping between series and their theme songs
   - This approach works with read-only media directories (like Docker containers)
   - No writing to media directories is required
   - Themes are stored only in your custom path and served directly from there

### Using Environment Variable
You can set the `JELLYFIN_THEME_SONGS_PATH` environment variable to override the theme path:

**For Docker:**
```
-e JELLYFIN_THEME_SONGS_PATH=/path/to/themes
```

**For systemd:**
```bash
# Edit the service file
sudo systemctl edit jellyfin.service
# Add the following line
Environment="JELLYFIN_THEME_SONGS_PATH=/path/to/themes"
# Restart Jellyfin
sudo systemctl restart jellyfin
```

### Using the Setup Script
Run the included setup script to configure the theme path:
```bash
chmod +x setup_theme_link.sh
sudo ./setup_theme_link.sh
```
Edit the script first to set your desired path.





## Build Process
1. Clone or download this repository
2. Ensure you have .NET Core SDK setup and installed
3. Build plugin with following command.
```sh
dotnet publish --configuration Release --output bin
```
4. Place the resulting .dll file in a folder called ```plugins/Theme Songs``` under  the program data directory or inside the portable install directory


