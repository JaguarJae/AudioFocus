using Windows.Media.Control;
using Microsoft.Win32;

namespace AudioFocus
{
    static class AudioFocus
    {
        static List<GlobalSystemMediaTransportControlsSession> PlayingSessions;
        static GlobalSystemMediaTransportControlsSession? activeSession;
        static GlobalSystemMediaTransportControlsSession? backSession;
        static GlobalSystemMediaTransportControlsSession? spotifySession;
        static GlobalSystemMediaTransportControlsSession[] allSessions;
        static GlobalSystemMediaTransportControlsSessionManager manager;
        static bool alwaysSomethingPlaying = true;
        static bool audioFocusActive = true;
        static async Task Main(string[] args)
        {
            await EventSubscriptions();
            spotifySession = GetSpotifySession();
            activeSession = await GetActiveSession();
            while (true)
            {
                await Debug(500);
            }
        }
        static async Task Debug(int delay)
        {
            Console.WriteLine("Active Session: " + activeSession?.SourceAppUserModelId);
            Console.WriteLine("Back Session: " + backSession?.SourceAppUserModelId);
            Console.WriteLine(" ");
            await Task.Delay(delay);
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
                session.PlaybackInfoChanged += async (s, e) => await OnSessionPlaybackChange(s, e);
            }
        }
        static async void OnWindowsLockSwitch(object sender, SessionSwitchEventArgs e)
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
                await Task.Delay(500);
                if (spotifySession != null) await spotifySession.TryPlayAsync();
                activeSession = spotifySession;
                audioFocusActive = true;
            }
        }
        static async Task OnSessionPlaybackChange(GlobalSystemMediaTransportControlsSession sender, PlaybackInfoChangedEventArgs args)
        {
            if (audioFocusActive)
            {
                if (sender.GetPlaybackInfo().PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing)
                {
                    Console.WriteLine(sender.SourceAppUserModelId + " has been played");
                    Console.WriteLine("Active Session!!" + activeSession?.SourceAppUserModelId);
                    if (activeSession != sender && activeSession != null)
                    {
                        Console.WriteLine("Mala notisia");
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
                        backSession = sender;
                    }
                }
            }
        }
        static async void OnSessionListChange(GlobalSystemMediaTransportControlsSessionManager sender, SessionsChangedEventArgs args)
        {
            foreach (var session in allSessions)
            {
                session.PlaybackInfoChanged -= async (s, e) => await OnSessionPlaybackChange(s, e);
            }
            allSessions = sender.GetSessions().ToArray();
            foreach (var session in allSessions)
            {
                session.PlaybackInfoChanged += async (s, e) => await OnSessionPlaybackChange(s, e);
            }
            spotifySession = GetSpotifySession();
            activeSession = await GetActiveSession();
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
    }
}