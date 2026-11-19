using Windows.Media.Control;
using Microsoft.Win32;
using System.Reflection;
using WindowsShortcutFactory;

namespace AudioFocus
{
    static class AudioFocus
    {
        static SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);

        static GlobalSystemMediaTransportControlsSession[]? allSessions;
        static GlobalSystemMediaTransportControlsSessionManager manager;

        static List<GlobalSystemMediaTransportControlsSession> PlayingSessions;
        static GlobalSystemMediaTransportControlsSession? activeSession;
        static GlobalSystemMediaTransportControlsSession? backSession;
        static GlobalSystemMediaTransportControlsSession? spotifySession;

        static bool alwaysPlaying = true;
        static bool audioFocusActive = true;
        static bool startOnStartup = true;
        static int lockDelay = 500;
        static int unlockDelay = 1000;

        static int silenceInterrupt = 0; // To fix playing and stopping session rapidly bug


        static Icon onIcon;
        static Icon offIcon;

        static Dictionary<string, string> config;
        static string configPath;

        static async Task Main(string[] args)
        {
            config = new Dictionary<string, string>();
            config = ReadConfig();

            LoadConfig();
            SetAutoStartup(startOnStartup);

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
            ToolStripMenuItem activateItem = new ToolStripMenuItem("Deactivate");
            ToolStripMenuItem settingsItem = new ToolStripMenuItem("Settings");
            ToolStripMenuItem quitItem = new ToolStripMenuItem("Quit");

            stopAllItem.Click += (s, e) =>
            {
                StopEverythingBut(null);
            };

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

            settingsItem.Click += (s, e) =>
            {
                using var settings = new SettingsForm();
                settings.ShowDialog();
            };

            quitItem.Click += (s, e) =>
            {
                trayIcon.Visible = false;
                Application.Exit();
            };

            menu.Items.Add(stopAllItem);
            menu.Items.Add(activateItem);
            menu.Items.Add(settingsItem);
            menu.Items.Add(quitItem);

            trayIcon.ContextMenuStrip = menu;

            Application.Run();
        }
        static Dictionary<string, string> ReadConfig()
        {
            string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AudioFocus");

            Directory.CreateDirectory(folder);

            configPath = Path.Combine(folder, "config.cfg");
            Dictionary<string, string> configFile = [];

            if (!File.Exists(configPath))
            {
                SaveConfig();
            }

            foreach (var line in File.ReadLines(configPath))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (line.StartsWith("#")) continue;

                var parts = line.Split('=', 2);

                if (parts.Length != 2) continue;

                string key = parts[0].Trim();
                string value = parts[1].Trim();

                configFile[key] = value;
            }

            return configFile;
        }

        internal static void SetAutoStartup(bool activate)
        {
            string startupPath = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
            string linkPath = Path.Combine(startupPath, "AudioFocus.lnk");
            if (activate)
            {
                string? workingPath = Environment.ProcessPath;
                if (workingPath != null)
                {
                    var shortcut = new WindowsShortcut();
                    shortcut.Path = workingPath;
                    shortcut.WorkingDirectory = Path.GetDirectoryName(workingPath);
                    shortcut.Save(linkPath);
                }
            }
            else
            {
                if (File.Exists(linkPath))
                    File.Delete(linkPath);
            }
        }

        internal static (bool alwaysPlaying, bool startOnStartup, int lockDelay, int unlockDelay) GetSettings()
        {
            return (alwaysPlaying, startOnStartup, lockDelay, unlockDelay);
        }

        internal static void UpdateSettings(bool newAlwaysPlaying, bool newStartOnStartup, int newLockDelay, int newUnlockDelay)
        {
            alwaysPlaying = newAlwaysPlaying;
            lockDelay = newLockDelay;
            unlockDelay = newUnlockDelay;
            startOnStartup = newStartOnStartup;

            SaveConfig();
        }

        static void SaveConfig()
        {
            config["alwaysPlaying"] = alwaysPlaying.ToString();
            config["startOnStartup"] = startOnStartup.ToString();
            config["lockDelay"] = lockDelay.ToString();
            config["unlockDelay"] = unlockDelay.ToString();

            File.WriteAllLines(configPath,config.Select(kvp => $"{kvp.Key}={kvp.Value}"));
        }
        static void LoadConfig()
        {
            bool save = false;

            if (!bool.TryParse(config["alwaysPlaying"], out alwaysPlaying))
            {
                alwaysPlaying = true;
                save = true;
            }
            if (!bool.TryParse(config["startOnStartup"], out startOnStartup))
            {
                startOnStartup = true;
                save = true;
            }
            if (!int.TryParse(config["lockDelay"], out lockDelay))
            {
                lockDelay = 500;
                save = true;
            }
            if (!int.TryParse(config["unlockDelay"], out unlockDelay))
            {
                unlockDelay = 1000;
                save = true;
            }
            if (save)
                SaveConfig();
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
                    await Task.Delay(lockDelay);
                    await StopEverythingBut(null);
                }
                else if (e.Reason == SessionSwitchReason.SessionUnlock)
                {
                    Log("Windows Unlocked");

                    if (spotifySession != null && alwaysPlaying)
                    {
                        await Task.Delay(unlockDelay);
                        await spotifySession?.TryPlayAsync();
                    }

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
                    var senderPlaybackStatus = sender.GetPlaybackInfo().PlaybackStatus;

                    if (senderPlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing)
                    {
                        Log(sender.SourceAppUserModelId + " has been played");

                        if (sender != activeSession)
                        {
                            if (activeSession != null)
                                await activeSession.TryPauseAsync();

                            backSession = activeSession;
                            activeSession = sender;

                        }
                    }
                    else if (senderPlaybackStatus != GlobalSystemMediaTransportControlsSessionPlaybackStatus.Changing || senderPlaybackStatus != GlobalSystemMediaTransportControlsSessionPlaybackStatus.Opened)
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
                                    activeSession = backSession;
                                    backSession = sender;
                                    await activeSession.TryPlayAsync();
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
                for (int i = 0; i < allSessions.Length; i++)
                {
                    GlobalSystemMediaTransportControlsSession? session = allSessions[i];
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
        static GlobalSystemMediaTransportControlsSession[] GetAllSessions() => manager.GetSessions().ToArray();
        static GlobalSystemMediaTransportControlsSession? GetSpotifySession() => allSessions.FirstOrDefault(s => s.SourceAppUserModelId?.Contains("spotify", StringComparison.CurrentCultureIgnoreCase) == true);

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
