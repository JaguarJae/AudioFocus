using System;
using Windows.Media.Control;

namespace AudioFocus
{
    class AudioFocus
    {
        static async Task Main(string[] args)
        {
            GlobalSystemMediaTransportControlsSession spotifySession = null;

            bool spotifyPlaying = false;
            bool otherPlaying = false;
            
            async Task Sessions()
            {
                var manager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
                var sessions = manager.GetSessions();

                foreach (var session in sessions)
                {
                    var status = session.GetPlaybackInfo().PlaybackStatus;

                    if (status == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing)
                    {
                        if (session.SourceAppUserModelId.Contains("Spotify"))
                        {
                            spotifySession = session;
                            spotifyPlaying = true;
                        }
                        else
                        {
                            otherPlaying = true;
                        }
                    }
                }
            }

            await Sessions();

            Console.ReadLine();
        }
    }
}