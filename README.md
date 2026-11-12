# AudioFocus

**AudioFocus** is a lightweight Windows system tray application that automatically manages media playback, ensuring that only one application plays audio at a time.

It prioritizes the most recently activated media session, automatically pausing other applications to prevent overlapping audio.

## Features

- **Automatic Audio Focus:** Automatically pauses the previously active media session when another application starts playing.
- **Session Management:** Detects and monitors active media sessions using the Windows Global System Media Transport Controls API.
- **Playback Restoration:** Optionally resumes the previous media session when the current one is paused.
- **Spotify Integration:** Can automatically resume Spotify after unlocking Windows.
- **Windows Lock Detection:** Pauses media playback when the Windows session is locked.
- **System Tray Integration:** Runs in the background with a system tray icon and context menu.
- **Quick Controls:** Activate or deactivate AudioFocus, stop all playback, or exit the application directly from the tray.
- **Configurable Settings:** Stores application settings in a simple configuration file.

## How It Works

AudioFocus monitors Windows media sessions and listens for playback state changes.

When an application starts playing:

1. AudioFocus detects the new playback session.
2. The previously active session is automatically paused.
3. The new session becomes the active one.

When the active session is paused, AudioFocus can optionally resume the previous session after a short delay. This prevents unnecessary interruptions when switching between applications.

The application also monitors Windows lock and unlock events, pausing playback when the computer is locked and optionally resuming Spotify after unlocking.

## Requirements

- Windows 10 / Windows 11
- .NET 8
- Windows Media Control API support

## Configuration

AudioFocus automatically creates its configuration directory on first launch:

```text
%LocalAppData%\AudioFocus\config.cfg
```

The configuration file uses a simple `key=value` format.

Available settings:

| Setting | Description |
|---|---|
| `alwaysPlaying` | Enables automatic playback restoration when the active session is paused. |
| `lockDelay` | Delay in milliseconds before stopping playback after Windows is locked. |
| `unlockDelay` | Delay in milliseconds before resuming Spotify after Windows is unlocked. |

## System Tray Controls

Right-click the AudioFocus icon in the system tray to access the following controls:

| Option | Description |
|---|---|
| Stop Everything | Pauses all currently playing media sessions. |
| Activate / Deactivate | Enables or disables automatic audio management. |
| Quit | Closes AudioFocus. |

The tray icon also indicates whether AudioFocus is currently active.

## Technologies

- **C#** – Main application logic.
- **.NET 8** – Application framework.
- **Windows.Media.Control** – Media session detection and playback control.
- **Microsoft.Win32** – Windows session lock/unlock event monitoring.
- **Windows Forms** – System tray integration.

## Building from Source

Clone the repository:

```bash
git clone https://github.com/YOUR_USERNAME/AudioFocus.git
cd AudioFocus
```

Build the project:

```bash
dotnet build -c Release
```

Run the application:

```bash
dotnet run
```

To publish a standalone Windows x64 executable:

```bash
dotnet publish -c Release -r win-x64 --self-contained true
```
