using System;
using Windows.Media.Control;

namespace AudioFocus
{
    class AudioFocus
    {
        static async Task Main(string[] args)
        {
            
            async Task Sessions()
            {
                var manager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
                var sessions = manager.GetSessions();

                foreach (var session in sessions)
                {
                    Console.WriteLine(session.SourceAppUserModelId);
                }
            }

            await Sessions();

            Console.ReadLine();
        }
    }
}