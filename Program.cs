using Windows.Media.Control;
using Microsoft.Win32;
using System.Reflection;

namespace AudioFocus
{
    static class AudioFocus
    {
        static SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);

        static GlobalSystemMediaTransportControlsSession[] allSessions;
        static GlobalSystemMediaTransportControlsSessionManager manager;

        static List<GlobalSystemMediaTransportControlsSession> PlayingSessions;
        static GlobalSystemMediaTransportControlsSession? activeSession;
        static GlobalSystemMediaTransportControlsSession? backSession;
        static GlobalSystemMediaTransportControlsSession? spotifySession;

        static bool alwaysPlaying = true;
        static bool audioFocusActive = true;

        static int silenceInterrupt = 0;

        static Icon onIcon;
        static Icon offIcon;
        static async Task Main(string[] args)
        {
            await EventSubscriptions();

            onIcon = LoadIcon("onIcon.ico");
            offIcon = LoadIcon("offIcon.ico");

            spotifySession = GetSpotifySession();
            activeSession = await GetActiveSession();

            CreateTray();
        }
        static Icon LoadIcon(string name)
        {
            var asm = Assembly.GetExecutingAssembly();
            using Stream? stream = asm.GetManifestResourceStream($"AudioFocus.{name}");

            if (stream != null) return new Icon(stream);
            else return SystemIcons.Application;
        }
        static void CreateTray()
        {
            NotifyIcon trayIcon = new NotifyIcon();

            trayIcon.Icon = onIcon;
            trayIcon.Visible = true;
            trayIcon.Text = "AudioFocus";

            ContextMenuStrip menu = new ContextMenuStrip();

            ToolStripMenuItem stopAllItem = new ToolStripMenuItem("Stop Everything");
            ToolStripMenuItem alwaysPlayingBox = new ToolStripMenuItem("Always Something Playing");
            ToolStripMenuItem activateItem = new ToolStripMenuItem("Deactivate");
            ToolStripMenuItem quitItem = new ToolStripMenuItem("Quit");

            activateItem.Click += (s, e) =>
            {
                Log("Changed to" + !audioFocusActive);
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

            stopAllItem.Click += (s, e) =>
            {
                StopEverythingBut(null);
            };

            alwaysPlayingBox.Click += (s, e) =>
            {
                alwaysPlaying = !alwaysPlaying;
                alwaysPlayingBox.Checked = alwaysPlaying;
            };

            menu.Items.Add(stopAllItem);
            menu.Items.Add(alwaysPlayingBox); alwaysPlayingBox.Checked = alwaysPlaying;
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
                Log("Session: " + session.SourceAppUserModelId + " is being targeted");
                session.PlaybackInfoChanged += OnSessionPlaybackChange;
            }
        }
        static async void OnWindowsLockSwitch(object sender, SessionSwitchEventArgs e)
        {
            await semaphore.WaitAsync();
            try
            {
                if (e.Reason == SessionSwitchReason.SessionLock)
                {
                    Log("Thread locked");
                    audioFocusActive = false;
                    Log("Windows Locked");
                    await Task.Delay(500);
                    await StopEverythingBut(null);
                }
                else if (e.Reason == SessionSwitchReason.SessionUnlock)
                {
                    Log("Thread locked");

                    Log("Windows Unlocked");
                    await Task.Delay(1000);
                    if (spotifySession != null) await spotifySession.TryPlayAsync();
                    activeSession = spotifySession;
                    backSession = null;
                    audioFocusActive = true;
                }
            }
            finally
            {
                semaphore.Release();
                Log("Thread free");
            }
        }
        static async void OnSessionPlaybackChange(GlobalSystemMediaTransportControlsSession sender, PlaybackInfoChangedEventArgs args)
        {
            silenceInterrupt++;
            Log("Silence Interrupted: " + silenceInterrupt);

            await semaphore.WaitAsync();
            Log("Thread locked");

            try
            {
                if (audioFocusActive)
                {

                    if (sender.GetPlaybackInfo().PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing)
                    {
                        Log(sender.SourceAppUserModelId + " has been played");

                        if (sender != activeSession)
                        {
                            if (activeSession != null) await activeSession.TryPauseAsync();

                            backSession = activeSession;
                            activeSession = sender;
                        }
                    }
                    else
                    {
                        Log(sender.SourceAppUserModelId + " has been paused");
                        if (sender == activeSession)
                        {
                            silenceInterrupt = 0;
                            if (alwaysPlaying && backSession != null)
                            {
                                await Task.Delay(400);
                                if (silenceInterrupt == 0)
                                {
                                    Log("essential back session" + backSession.SourceAppUserModelId);
                                    activeSession = backSession;
                                    backSession = sender;
                                    await activeSession.TryPlayAsync();
                                }
                                else
                                {
                                    return;
                                }
                            }
                            else
                            { 
                                activeSession = null;
                                backSession = sender;
                            }
                        }
                    }
                }
            }
            finally
            {
                semaphore.Release();
                Log("\nActive Session: " + activeSession?.SourceAppUserModelId);
                Log("Back Session: " + backSession?.SourceAppUserModelId + "\n");
                Log("Thread free");

            }
        }
        static async void OnSessionListChange(GlobalSystemMediaTransportControlsSessionManager sender, SessionsChangedEventArgs args)
        {
            await semaphore.WaitAsync();
            Log("Thread locked");

            try
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
            finally
            {
                semaphore.Release();
                Log("Thread free");

            }
        }
        static async Task StopEverythingBut(GlobalSystemMediaTransportControlsSession? exceptionSession)
        {
            var sessions = manager.GetSessions();

            activeSession = exceptionSession;
            backSession = null;

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
            return allSessions.FirstOrDefault(s => s.SourceAppUserModelId?.ToLower().Contains("spotify") == true);
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

        [System.Diagnostics.Conditional("DEBUG")]
        static void Log(string msg)
        {
            Console.WriteLine(msg);
        }
    }
}