using Windows.Media.Control;

namespace AudioFocus
{
    static class AudioFocus
    {
        static GlobalSystemMediaTransportControlsSession? activeSession;
        static GlobalSystemMediaTransportControlsSession? spotifySession;
        static GlobalSystemMediaTransportControlsSession[]? otherSession;
        static GlobalSystemMediaTransportControlsSession[] allSessions;
        static GlobalSystemMediaTransportControlsSessionManager manager;
        enum States
        {
            Spotify,
            Other,
            Multiple,
            Nothing,
        }
        static async Task Main(string[] args)
        {
            manager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
            manager.SessionsChanged += OnSessionsChange;

            allSessions = GetAllSessions();

            while (allSessions == null)
            {
                allSessions = GetAllSessions();
                await Task.Delay(500);
            }
            spotifySession = GetSpotifySession();
            otherSession = GetOtherSessions();

            oldState = AudioState();

            while(true)
            {
                foreach (var session in allSessions)
                {
                    //Console.WriteLine(session.SourceAppUserModelId + ": " + session.GetPlaybackInfo().PlaybackStatus.ToString());
                }

                //Console.WriteLine(AudioState());

                await Task.Delay(500);
            }
        }
        static void OnSessionsChange(GlobalSystemMediaTransportControlsSessionManager sender, SessionsChangedEventArgs args)
        {
            allSessions = sender.GetSessions().ToArray();


        }
        static States AudioState()
        {
            if (spotifySession.GetPlaybackInfo().PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing)
            {
                if (OtherPlaying() == 0)
                {
                    activeSession = spotifySession;
                    return States.Spotify;
                }
                else
                {
                    return States.Multiple;
                }
            }
            else if (OtherPlaying() > 0)
            {
                return States.Other;
            }
            else
            {
                return States.Nothing;
            }
        }
        static int OtherPlaying()
        {
            int playing = 0;

            for (int i = 0; i < otherSession.Length; i++)
            {
                if (otherSession[i].GetPlaybackInfo().PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing)
                {
                    playing++;
                }
            }
            return playing;
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

        /*async Task StopEverything()
        {
            var sessions = manager.GetSessions();

            foreach (var session in sessions)
            {
                if (session.GetPlaybackInfo().PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing)
                {
                    await session.TryPauseAsync();
                }
            }
        }*/
    }
}