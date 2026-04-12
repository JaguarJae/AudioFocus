using Windows.Media.Control;

namespace AudioFocus
{
    static class AudioFocus
    {
        static List<GlobalSystemMediaTransportControlsSession> PlayingSessions;
        static GlobalSystemMediaTransportControlsSession? activeSession;
        static GlobalSystemMediaTransportControlsSession? backSession;
        static GlobalSystemMediaTransportControlsSession[] allSessions;
        static GlobalSystemMediaTransportControlsSessionManager manager;
        static bool alwaysPlaying = false;
        static float silenceTime = 2;
        static async Task Main(string[] args)
        {
            manager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
            manager.SessionsChanged += OnSessionListChange;

            allSessions = GetAllSessions();
            foreach (var session in allSessions)
            {
                session.PlaybackInfoChanged += async (s, e) => await OnSessionPlaybackChange(s, e);
            }

            activeSession = await GetActiveSession();

            while (true)
            {
                Console.Clear();
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
        static async Task OnSessionPlaybackChange(GlobalSystemMediaTransportControlsSession sender, PlaybackInfoChangedEventArgs args)
        {
            if (sender.GetPlaybackInfo().PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing)
            {
                if (activeSession != sender && activeSession != null)
                {
                    await activeSession.TryPauseAsync();
                    backSession = activeSession;
                    activeSession = sender;
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