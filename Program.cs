using Windows.Media.Control;
using Microsoft.Win32;
using System.Reflection;

namespace AudioFocus
{
    static class AudioFocus
    {
        static GlobalSystemMediaTransportControlsSession[] allSessions;
        static GlobalSystemMediaTransportControlsSessionManager manager;

        static List<GlobalSystemMediaTransportControlsSession> PlayingSessions;
        static GlobalSystemMediaTransportControlsSession? activeSession;
        static GlobalSystemMediaTransportControlsSession? backSession;
        static GlobalSystemMediaTransportControlsSession? spotifySession;

        static bool alwaysSomethingPlaying = true;
        static bool audioFocusActive = true;

        static Icon onIcon;
        static Icon offIcon;
        static async Task Main(string[] args)
        {
            await EventSubscriptions();

            onIcon = LoadIcon("onIcon.ico");
            offIcon = LoadIcon("offIcon.ico");
            CreateTray();
        }
        static Icon LoadIcon(string name)
        {
            var asm = Assembly.GetExecutingAssembly();
            Stream stream = asm.GetManifestResourceStream($"AudioFocus.{name}");
            return new Icon(stream);
        }
        static void CreateTray()
        {
            NotifyIcon trayIcon = new NotifyIcon();

            trayIcon.Icon = onIcon;
            trayIcon.Visible = true;
            trayIcon.Text = "AudioFocus";

            ContextMenuStrip menu = new ContextMenuStrip();

            ToolStripMenuItem activateItem = new ToolStripMenuItem("Deactivate");
            ToolStripMenuItem quitItem = new ToolStripMenuItem("Quit");

            activateItem.Click += (s, e) =>
             {
                 Console.WriteLine("Changed to" + !audioFocusActive);
                 audioFocusActive = !audioFocusActive;
                 if (audioFocusActive)
                 {
                     activateItem.Text = "Deactivate";
                     trayIcon.Icon = onIcon;
                 }
                 else
                 {
                     activateItem.Text = "Activate";
                     trayIcon.Icon = offIcon;
                 }
             };

            quitItem.Click += (s, e) =>
            {
                trayIcon.Visible = false;
                Application.Exit();
            };

            menu.Items.Add(activateItem);
            menu.Items.Add(quitItem);

            trayIcon.ContextMenuStrip = menu;

            Application.Run();
        }
        static async Task EventSubscriptions()
        {
            manager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
            manager.SessionsChanged += OnSessionListChange;
            SystemEvents.SessionSwitch += OnWindowsLockSwitch;

            allSessions = GetAllSessions();
            foreach (var session in allSessions)
            {
                Console.WriteLine("Session: " + session.SourceAppUserModelId + " is being targeted");
                session.PlaybackInfoChanged += OnSessionPlaybackChange;
            }
        }
        static async void OnWindowsLockSwitch(object sender, SessionSwitchEventArgs e)
        {
            if (audioFocusActive)
            {
                if (e.Reason == SessionSwitchReason.SessionLock)
                {
                    audioFocusActive = false;
                    Console.WriteLine("Windows Locked");
                    await StopEverythingBut(null);
                }
                else if (e.Reason == SessionSwitchReason.SessionUnlock)
                {
                    Console.WriteLine("Windows Unlocked");
                    await Task.Delay(1000);
                    if (spotifySession != null) await spotifySession.TryPlayAsync();
                    activeSession = spotifySession;
                    backSession = null;
                    audioFocusActive = true;
                }
            }
        }
        static async void OnSessionPlaybackChange(GlobalSystemMediaTransportControlsSession sender, PlaybackInfoChangedEventArgs args)
        {
            if (audioFocusActive)
            {
                if (sender.GetPlaybackInfo().PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing)
                {
                    Console.WriteLine(sender.SourceAppUserModelId + " has been played");
                    if (activeSession != sender && activeSession != null)
                    {
                        await activeSession.TryPauseAsync();
                        backSession = activeSession;
                    }
                    activeSession = sender;
                }
                else
                {
                    Console.WriteLine(sender.SourceAppUserModelId + " has been paused");
                    if (sender == activeSession)
                    {
                        if (alwaysSomethingPlaying && backSession != null)
                        {
                            activeSession = backSession;
                            await Task.Delay(500);
                            await activeSession.TryPlayAsync();
                        }
                        else activeSession = null;
                        backSession = sender;
                    }
                }
            }
        }
        static async void OnSessionListChange(GlobalSystemMediaTransportControlsSessionManager sender, SessionsChangedEventArgs args)
        {
            foreach (var session in allSessions)
            {
                session.PlaybackInfoChanged -= OnSessionPlaybackChange;
            }
            allSessions = sender.GetSessions().ToArray();
            foreach (var session in allSessions)
            {
                session.PlaybackInfoChanged += OnSessionPlaybackChange;
            }
            spotifySession = GetSpotifySession();
            activeSession = await GetActiveSession();
        }
        static async Task StopEverythingBut(GlobalSystemMediaTransportControlsSession? exceptionSession)
        {
            var sessions = manager.GetSessions();

            foreach (var session in sessions)
            {
                if (session.GetPlaybackInfo().PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing && session != exceptionSession)
                {
                    await session.TryPauseAsync();
                }
            }
        }
        static List<GlobalSystemMediaTransportControlsSession> GetPlayingSessions()
        {
            List<GlobalSystemMediaTransportControlsSession> localPlayingSessions = new List<GlobalSystemMediaTransportControlsSession>();

            for (int i = 0; i < allSessions.Length; i++)
            {
                if (allSessions[i].GetPlaybackInfo().PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing)
                {
                    localPlayingSessions.Add(allSessions[i]);
                }
            }
            return localPlayingSessions;
        }
        static GlobalSystemMediaTransportControlsSession[] GetAllSessions()
        {
            return manager.GetSessions().ToArray();
        }
        static GlobalSystemMediaTransportControlsSession? GetSpotifySession()
        {
            return allSessions.FirstOrDefault(s => s.SourceAppUserModelId?.Contains("Spotify") == true);
        }
        static async Task<GlobalSystemMediaTransportControlsSession?> GetActiveSession()
        {
            PlayingSessions = GetPlayingSessions();

            switch (PlayingSessions.Count)
            {
                case < 1:
                    return null;

                case 1:
                    return PlayingSessions.First();

                case > 1:
                    return await ChooseActiveSession();
            }
        }
        static async Task<GlobalSystemMediaTransportControlsSession> ChooseActiveSession()
        {
            PlayingSessions = GetPlayingSessions();

            var chosenSession = PlayingSessions.First();
            await StopEverythingBut(chosenSession);
            return chosenSession;
        }
    }
}