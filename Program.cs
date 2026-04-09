using Windows.Media.Control;

namespace AudioFocus
{
    class AudioFocus
    {
        static async Task Main(string[] args)
        {
            var manager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
            var spotifySession = TryGetSession("Spotify");

            if (true)
            {
                if (spotifySession != null)
                {
                    Console.WriteLine(SpotifyPlaying());
                }
            }

            bool SpotifyPlaying()
            {
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

            Console.ReadLine();
        }
    }
}