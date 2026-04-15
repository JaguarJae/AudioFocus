🎧 AudioFocus

AudioFocus is a Windows application built in C# that automatically manages which app should be playing audio at any given time.

When multiple applications (like Spotify, browsers, or media players) try to play sound simultaneously, AudioFocus intelligently selects the correct one and pauses the rest.

✨ Features
🔍 Automatic detection of active media sessions
🎯 Smart audio focus prioritization (e.g. Spotify over others)
⏸️ Auto-pausing of background audio sources
🔄 Real-time updates when playback changes
🧩 Built on top of Windows media session APIs
🧠 How It Works

AudioFocus relies on the Windows media control system:

GlobalSystemMediaTransportControlsSessionManager
GlobalSystemMediaTransportControlsSession
Core logic:
Retrieve all active media sessions
Detect which sessions are currently playing
Determine the “active” session based on priority
Pause all other sessions
React to changes using event listeners (PlaybackInfoChanged)

This allows AudioFocus to dynamically adapt as users switch between apps.

🚀 Installation
Option 1: Download executable

Go to the Releases section and download the latest version.

Option 2: Build from source
git clone https://github.com/yourusername/audiofocus.git
cd audiofocus
dotnet build
dotnet run
▶️ Usage
Launch AudioFocus
Open multiple audio sources (Spotify, YouTube, etc.)
The app will automatically manage which one should play

No manual interaction required.

🛠️ Tech Stack
C# (.NET 8)
Windows Media Control API
WinForms (system tray support)
⚠️ Known Limitations
⏱️ Some applications (like Spotify) may have slight delay in state updates
🪟 Only works on Windows (uses native APIs)
🔄 Session detection depends on system-level media integration
