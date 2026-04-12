using Windows.Media.Control;

namespace AudioFocus
{
    static class AudioFocus
    {
        static List<GlobalSystemMediaTransportControlsSession> PlayingSessions;
        static GlobalSystemMediaTransportControlsSession? activeSession;
        static GlobalSystemMediaTransportControlsSession? backSession;
        static GlobalSystemMediaTransportControlsSession? spotifySession;
        static GlobalSystemMediaTransportControlsSession[]? otherSessions;
        static GlobalSystemMediaTransportControlsSession[] allSessions;
        static GlobalSystemMediaTransportControlsSessionManager manager;
        static bool test = false;
        static bool alwaysPlaying = false;
        static float silenceTime = 2;
        enum States
        {
            Single,
            Multiple,
            Nothing,
        }
        static async Task Main(string[] args)
        {
            manager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
            manager.SessionsChanged += OnSessionListChange;

            allSessions = GetAllSessions();
            foreach (var session in allSessions)
            {
                session.PlaybackInfoChanged += async (s, e) => await OnSessionPlaybackChange(s, e);
            }

            //spotifySession = GetSpotifySession();
            //otherSessions = GetOtherSessions();
            activeSession = GetActiveSession().Result;

            while (true)
            {
                Console.WriteLine("Active Session: " + activeSession?.SourceAppUserModelId);
                Console.WriteLine("Back Session: " + backSession?.SourceAppUserModelId);

                await Task.Delay(500);
            }
        }
        static async Task<GlobalSystemMediaTransportControlsSession?> GetActiveSession()
        {
            PlayingSessions = GetPlayingSessions();
            switch (PlayingSessions.Count)
            {
                case < 1:
                    Console.WriteLine("None to decide");
                    return null;

                case 1:
                    Console.WriteLine("Easy decision");
                    return PlayingSessions.First();

                case > 1:
                    Console.WriteLine("Gotta choose best sound");
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
        static async Task OnSessionPlaybackChange(GlobalSystemMediaTransportControlsSession sender, PlaybackInfoChangedEventArgs args)
        {
            if (sender.GetPlaybackInfo().PlaybackStatus != GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing)
            {
                if (activeSession == sender)
                {
                    if (alwaysPlaying && backSession != null)
                    {
                        await backSession.TryPlayAsync();
                        activeSession = backSession;
                    }
                    else activeSession = null;
                }

                backSession = sender;
            }
            else
            {
                if (activeSession != null && activeSession != sender) await activeSession.TryPauseAsync();
                activeSession = sender;

                if (sender == backSession) backSession = null;
            }
        }
        static async void OnSessionListChange(GlobalSystemMediaTransportControlsSessionManager sender, SessionsChangedEventArgs args)
        {
            Console.WriteLine("Session List Change!!");
            foreach (var session in allSessions)
            {
                session.PlaybackInfoChanged -= async (s, e) => await OnSessionPlaybackChange(s, e);
            }
            allSessions = sender.GetSessions().ToArray();
            foreach (var session in allSessions)
            {
                session.PlaybackInfoChanged += async (s, e) => await OnSessionPlaybackChange(s, e);
            }
            activeSession = await GetActiveSession();
        }
        static States AudioState()
        {
            List<GlobalSystemMediaTransportControlsSession> totalPlaying = GetPlayingSessions();

            switch (totalPlaying.Count)
            {
                case < 1:
                    return States.Nothing;
                case 1:
                    return States.Single;
                case > 1:
                    return States.Multiple;
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
        static GlobalSystemMediaTransportControlsSession[] GetOtherSessions()
        {
            return allSessions.Where(s => !s.SourceAppUserModelId?.Contains("Spotify") == true).ToArray();
        }
        static GlobalSystemMediaTransportControlsSession? GetSpotifySession()
        {
            foreach (var session in allSessions)
            {
                if (session.SourceAppUserModelId?.Contains("Spotify") == true)
                {
                    return session;
                }
            }
            return null;
        }
        static async Task StopEverythingBut(GlobalSystemMediaTransportControlsSession exceptionSession)
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