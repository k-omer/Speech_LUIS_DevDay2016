using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Media.Playback;
using Windows.Media.SpeechSynthesis;

namespace PizzaBot.ServiceHelpers
{
    public static class TextToSpeechService
    {
        private static readonly SpeechSynthesizer SpeechSynthesizer = new SpeechSynthesizer();
        private static readonly SemaphoreSlim Semaphore = new SemaphoreSlim(0, 1);
        private static bool _isInitialized;

        /// <summary>
        /// Allows the computer to speak with a given a text
        /// </summary>
        public static async Task SayAsync(string textToSay)
        {
            Initialize();

            var speechStream = await SpeechSynthesizer.SynthesizeTextToStreamAsync(textToSay);
            BackgroundMediaPlayer.Current.SetStreamSource(speechStream);

            await Semaphore.WaitAsync();
        }

        public static void Initialize()
        {
            if (!_isInitialized)
            {
                BackgroundMediaPlayer.Current.MediaEnded += OnMediaEnded;
                _isInitialized = true;
            }
        }

        public static void Interrupt()
        {
            if (_isInitialized)
            {
                BackgroundMediaPlayer.Shutdown();
                _isInitialized = false;
            }
        }

        private static void OnMediaEnded(MediaPlayer sender, object args)
        {
            Semaphore.Release();
        }
    }
}
