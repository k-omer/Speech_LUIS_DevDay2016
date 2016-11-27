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
                await StartVoiceRecognitionAsync();
            }
            catch(Exception e)
            {
                Debug.WriteLine(e.Message);
            }
        }

        private async void _speechRecognizer_HypothesisGenerated(SpeechRecognizer sender, SpeechRecognitionHypothesisGeneratedEventArgs args)
        {
            _voiceResult = args.Hypothesis.Text;

            await Dispatcher.RunAsync(CoreDispatcherPriority.High, () =>
            {
                TextMessage.Text = args.Hypothesis.Text;
            });
        }

        private void _speechRecognizer_StateChanged(SpeechRecognizer sender, SpeechRecognizerStateChangedEventArgs args)
        {
            Debug.WriteLine($"State : {args.State}");
            switch (args.State)
            {
                case SpeechRecognizerState.SoundEnded:
                    if (!string.IsNullOrEmpty(_voiceResult))
                    {
                        AddMessageInScreen(_voiceResult, true);
                        SendToLuisAsync();
                    }
                    break;
                case SpeechRecognizerState.Idle:
                    break;
            }
        }

        private async Task CancelVoiceRecognitionAsync()
        {
            try
            {
                Dispatcher.RunAsync(CoreDispatcherPriority.High, () =>
                {
                    TextMessage.Text = "";
                    ButtonMicrophone.Background = new SolidColorBrush(Windows.UI.Colors.Black);
                });
                await _speechRecognizer.ContinuousRecognitionSession.CancelAsync();

            }
            catch(Exception e)
            {
                Debug.WriteLine(e.Message);
            }
        }

        private async Task StartVoiceRecognitionAsync()
        {
            try
            {
                await _speechRecognizer.ContinuousRecognitionSession.StartAsync();
                Dispatcher.RunAsync(CoreDispatcherPriority.High, () =>
                {
                    TextMessage.Text = "";
                    ButtonMicrophone.Background = new SolidColorBrush(Windows.UI.Colors.YellowGreen);
                });

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
                switch (luisResult.TopScoringIntent.Name)
                {
                    case "OrderPizza":
                        sentence = "Sure, which ingredients do you want in your pizza?";
                        break;
                    case "WhichIngredients":
                        sentence = OrderPizza(luisResult.Intents.First().Actions.First().Parameters.First().ParameterValues);
                        break;
                    case "ValidateOrder":
                        sentence = "Ok, your order is confirmed!";
                        break;
                    case "GetHello":
                        sentence = "Hello there ! What can I do for you?";
                        break;
                    case "GetGoodBye":
                        sentence = "Have a nice day!";
                        break;
                    default:
                        sentence = "I am not sure I can understand that";
                        break;
                }
                AddMessageInScreen(sentence, false);
                await TextToSpeechService.SayAsync(sentence);

            }

            _voiceResult = "";
            await StartVoiceRecognitionAsync();
        }

        private async void AddMessageInScreen(string message, bool right)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                StackPanel stack = new StackPanel();
                stack.Margin = new Thickness(10);
                stack.Padding = new Thickness(15);
                stack.Width = 300;
                stack.BorderThickness = new Thickness(5, 5, 5, 5);

                TextBlock chatText = new TextBlock();

                chatText.FontFamily = new FontFamily("Segoe UI");
                chatText.TextWrapping = TextWrapping.WrapWholeWords;
                chatText.FontSize = 20;

                if (right == true)
                {
                    chatText.HorizontalAlignment = HorizontalAlignment.Right;
                    chatText.Foreground = new SolidColorBrush(Windows.UI.Colors.White);
                    stack.Background = new SolidColorBrush(Windows.UI.Colors.Tomato);
                    stack.HorizontalAlignment = HorizontalAlignment.Right;
                }
                else
                {
                    chatText.HorizontalAlignment = HorizontalAlignment.Left;
                    chatText.Foreground = new SolidColorBrush(Windows.UI.Colors.White);
                    stack.Background = new SolidColorBrush(Windows.UI.Colors.OrangeRed);
                    stack.HorizontalAlignment = HorizontalAlignment.Left;
                }

                chatText.Text = message;
                stack.Children.Add(chatText);
                ChatList.Children.Add(stack);

                ScrollChatViewer.Measure(ScrollChatViewer.RenderSize);
                ScrollChatViewer.ScrollToVerticalOffset(ScrollChatViewer.ScrollableHeight);
            });
        }

        private string OrderPizza(ParameterValue[] parameterValues)
        {
            string sentence = "You have ordered a pizza with :\n";

            parameterValues.ToList().ForEach(p =>
            {
                sentence += $"\t- {p.Entity}\n";
            });

            sentence += "\nAnything else ?";

            return sentence;
        }

        private void ButtonSendMessage_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(TextMessage.Text))
            {
                AddMessageInScreen(TextMessage.Text, true);
                _voiceResult = TextMessage.Text;
                SendToLuisAsync();
            }
        }
    }
}
