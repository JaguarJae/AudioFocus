using Windows.Media.Control;

namespace AudioFocus
{
    class AudioFocus
    {
        static async Task Main(string[] args)
        {
            var manager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
            GlobalSystemMediaTransportControlsSession spotifySession;
            GlobalSystemMediaTransportControlsSession otherSession;

            while (true)
            {
                if (IsSpotifyPlaying() && OtherPlaying())
                {

                }

                await Task.Delay(1000);
            }

            bool OtherPlaying()
            {
                var sessions = manager.GetSessions();
                otherSession = null;
                foreach (var session in sessions)
                {
                    if (session.GetPlaybackInfo().PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing && !session.SourceAppUserModelId.Contains("Spotify"))
                    {
                        otherSession = session;
                        return true;
                    }
                }
                return false;
            }

            bool IsSpotifyPlaying()
            {
                spotifySession = TryGetSession("Spotify");

                if (spotifySession.GetPlaybackInfo().PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }

            GlobalSystemMediaTransportControlsSession TryGetSession(string name)
            {
                var sessions = manager.GetSessions();
                foreach (var session in sessions)
                {
                    if (session.SourceAppUserModelId.Contains(name))
                    {
                        return session;
                    }
                }
                return null;
            }

            async Task StopEverything()
            {
                var sessions = manager.GetSessions();

                foreach (var session in sessions)
                {
                    if (session.GetPlaybackInfo().PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing)
                    {
                        await session.TryPauseAsync();
                    }
                }
            }

        }
    }
}