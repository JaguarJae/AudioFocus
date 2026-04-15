# 🎧 AudioFocus

**AudioFocus** is a Windows application built in C# that automatically manages which app should be playing audio at any given time.

When multiple applications (like Spotify, browsers, or media players) try to play sound simultaneously, AudioFocus intelligently selects the correct one and pauses the rest.

---

## ✨ Features

- 🔍 Automatic detection of active media sessions  
- 🎯 Smart audio focus prioritization (e.g. Spotify over others)  
- ⏸️ Auto-pausing of background audio sources  
- 🔄 Real-time updates when playback changes  
- 🧩 Built on top of Windows media session APIs  

---

## 🧠 How It Works

AudioFocus relies on the Windows media control system:

- `GlobalSystemMediaTransportControlsSessionManager`
- `GlobalSystemMediaTransportControlsSession`

### Core logic:

1. Retrieve all active media sessions  
2. Detect which sessions are currently playing  
3. Determine the “active” session based on priority  
4. Pause all other sessions  
5. React to changes using event listeners (`PlaybackInfoChanged`)  

---

## 🚀 Installation

### Option 1: Download executable

Go to the **Releases** section and download the latest version.

### Option 2: Build from source

```bash
git clone https://github.com/yourusername/audiofocus.git
cd audiofocus
dotnet build
dotnet run
