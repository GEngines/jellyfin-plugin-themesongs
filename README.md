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





## Build Process
1. Clone or download this repository
2. Ensure you have .NET Core SDK setup and installed
3. Build plugin with following command.
```sh
dotnet publish --configuration Release --output bin
```
4. Place the resulting .dll file in a folder called ```plugins/Theme Songs``` under  the program data directory or inside the portable install directory


