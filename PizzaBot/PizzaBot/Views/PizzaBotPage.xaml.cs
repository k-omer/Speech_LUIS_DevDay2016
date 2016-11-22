using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.SpeechRecognition;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Microsoft.Cognitive.LUIS;
using Coding4Fun.Toolkit.Controls;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using PizzaBot.ServiceHelpers;

namespace PizzaBot.Views
{
    public sealed partial class PizzaBotPage : Page
    {
        private SpeechRecognizer _speechRecognizer;
        private LuisClient _luisClient;
        private string _voiceResult = "";


        public PizzaBotPage()
        {
            this.InitializeComponent();

            _luisClient = new LuisClient("2fa72221-4611-4795-90b8-ec3f0396fc53", "5fdfc85a71e34146850e0ff08849520d");

            _speechRecognizer = new SpeechRecognizer();
            _speechRecognizer.Timeouts.EndSilenceTimeout = new TimeSpan(24, 0, 0);
            _speechRecognizer.Timeouts.InitialSilenceTimeout = new TimeSpan(24, 0, 0);

            _speechRecognizer.HypothesisGenerated += _speechRecognizer_HypothesisGenerated;
            _speechRecognizer.StateChanged += _speechRecognizer_StateChanged;

            StartListeningAsync();
       }

        private async void StartListeningAsync()
        {
            try
            {
                await _speechRecognizer.CompileConstraintsAsync();
                await _speechRecognizer.ContinuousRecognitionSession.StartAsync();
            }
            catch(Exception e)
            {
                Debug.WriteLine(e.Message);
            }
        }

        private void _speechRecognizer_HypothesisGenerated(SpeechRecognizer sender, SpeechRecognitionHypothesisGeneratedEventArgs args)
        {
            _voiceResult = args.Hypothesis.Text;
        }

        private void _speechRecognizer_StateChanged(SpeechRecognizer sender, SpeechRecognizerStateChangedEventArgs args)
        {
            Debug.WriteLine($"State : {args.State}");
            switch (args.State)
            {
                case SpeechRecognizerState.SoundEnded:
                    if(!string.IsNullOrEmpty(_voiceResult)) AddMessageInScreen(_voiceResult, true);
                    SendToLuisAsync();
                    break;
                case SpeechRecognizerState.Idle:
                    break;
            }
        }

        private async Task CancelVoiceRecognitionAsync()
        {
            try
            {
                await _speechRecognizer.ContinuousRecognitionSession.CancelAsync();
            }
            catch(Exception e)
            {
                Debug.WriteLine(e.Message);
            }
        }

        private async Task RestartVoiceRecognitionAsync()
        {
            try
            {
                await _speechRecognizer.ContinuousRecognitionSession.StartAsync();
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
            }
        }

        private async void SendToLuisAsync()
        {
            var sentence = "";
            await CancelVoiceRecognitionAsync();

            var luisResult = await _luisClient.Predict(_voiceResult);

            if (luisResult.Intents != null && luisResult.Intents.Any())
            {
                switch (luisResult.Intents.First().Name)
                {
                    case "OrderPizza":
                        sentence = "Sure, what ingredients do you want in your pizza ?";
                        break;
                    case "WhichIngredients":
                        sentence = OrderPizza(luisResult.Intents.First().Actions.First().Parameters.First().ParameterValues);
                        break;
                    case "ValidateOrder":
                        sentence = "Your order is confirmed !";
                        break;
                    case "GetHello":
                        sentence = "Hello there";
                        break; 
                    default:
                        sentence = "I am not sure I can understand that";
                        break;
                }
                AddMessageInScreen(sentence, false);
                await TextToSpeechService.SayAsync(sentence);

            }

            _voiceResult = "";
            await RestartVoiceRecognitionAsync();
        }

        private async void AddMessageInScreen(string message, bool right)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                TextBlock _chatBubble = new TextBlock();

                _chatBubble.Foreground = new SolidColorBrush(Windows.UI.Colors.BlueViolet);
                _chatBubble.FontFamily = new FontFamily("Segoe UI");
                _chatBubble.TextWrapping = TextWrapping.WrapWholeWords;

                if (right == true)
                {
                    _chatBubble.HorizontalAlignment = HorizontalAlignment.Right;
                    _chatBubble.Foreground = new SolidColorBrush(Windows.UI.Colors.BlueViolet);
                }
                else
                {
                    _chatBubble.HorizontalAlignment = HorizontalAlignment.Left;
                    _chatBubble.Foreground = new SolidColorBrush(Windows.UI.Colors.Blue);
                }

                _chatBubble.Text = message;
                ChatList.Children.Insert(0,_chatBubble);
            });
        }

        private string OrderPizza(ParameterValue[] parameterValues)
        {
            string sentence = "You have ordered a pizza with ";
            parameterValues.ToList().ForEach(p =>
            {
                sentence += $"{p.Entity}";
            });

            sentence += "\nAnything else you want me to add ?";

            return sentence;
        }
    }
}
