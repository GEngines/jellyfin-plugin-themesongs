/**
 * Theme Songs Volume Control Script
 * Adds volume control for theme songs played in Jellyfin
 */

(function() {
    // Default to 20% if not specified
    const themeVolume = window.ThemeSongsVolume || 20;
    
    // Set volume when audio elements with theme songs are created
    const originalCreateElement = document.createElement;
    document.createElement = function(tagName) {
        const element = originalCreateElement.call(document, tagName);
        
        // Only handle audio elements
        if (tagName.toLowerCase() === 'audio') {
            // Wait until the source is set
            const originalSetAttribute = element.setAttribute;
            element.setAttribute = function(name, value) {
                const result = originalSetAttribute.call(this, name, value);
                
                // Check if this is a theme audio
                if (name === 'src' && 
                    (value.includes('Audio/') || 
                     value.includes('theme') || 
                     value.toLowerCase().includes('theme.mp3'))) {
                    
                    console.log('Theme song detected, setting volume to ' + themeVolume + '%');
                    
                    // Set initial volume
                    this.volume = themeVolume / 100;
                    
                    // Also handle when it's about to play
                    this.addEventListener('loadedmetadata', function() {
                        this.volume = themeVolume / 100;
                    });
                    
                    this.addEventListener('play', function() {
                        this.volume = themeVolume / 100;
                    });
                }
                
                return result;
            };
        }
        
        return element;
    };
    
    // Also handle existing audio elements
    function setVolumeOnExistingElements() {
        const audioElements = document.querySelectorAll('audio');
        audioElements.forEach(audio => {
            // Check if this is likely a theme audio
            const src = audio.src || '';
            if (src.includes('Audio/') || 
                src.includes('theme') || 
                src.toLowerCase().includes('theme.mp3')) {
                
                audio.volume = themeVolume / 100;
            }
        });
    }
    
    // Set volume on page load
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', setVolumeOnExistingElements);
    } else {
        setVolumeOnExistingElements();
    }
    
    // Also handle when navigating to new pages within the app
    const observer = new MutationObserver(mutations => {
        for (const mutation of mutations) {
            if (mutation.addedNodes.length) {
                setVolumeOnExistingElements();
            }
        }
    });
    
    // Start observing once the document is loaded
    if (document.body) {
        observer.observe(document.body, { childList: true, subtree: true });
    } else {
        document.addEventListener('DOMContentLoaded', () => {
            observer.observe(document.body, { childList: true, subtree: true });
        });
    }
})();