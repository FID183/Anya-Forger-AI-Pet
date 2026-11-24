using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace DesktopCompanion
{
    public partial class MainframeWindow : Window
    {
        private class Snowflake
        {
            public required Ellipse Shape;
            public double X;
            public double Y;
            public double SpeedY;
            public double DriftX;
            public double Phase;
        }

        private readonly AppSettings _settings;
        private readonly Brain _brain;
        private readonly VoiceEngine _voice;
        private readonly Action _onSettingsApplied;
        private readonly List<Snowflake> _snowflakes = new();
        private readonly Random _rng = new();
        private bool _isInitializing = true;

        public MainframeWindow(AppSettings settings, Brain brain, VoiceEngine voice, Action onSettingsApplied)
        {
            InitializeComponent();
            _settings = settings;
            _brain = brain;
            _voice = voice;
            _onSettingsApplied = onSettingsApplied;

            LoadSettingsIntoUi();
            _isInitializing = false;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            InitSnowfall();
            CompositionTarget.Rendering += UpdateSnowfall;
        }

        private void Window_Closed(object? sender, EventArgs e)
        {
            CompositionTarget.Rendering -= UpdateSnowfall;
        }

        private void InitSnowfall()
        {
            SnowCanvas.Children.Clear();
            _snowflakes.Clear();

            for (int i = 0; i < 48; i++)
            {
                double size = _rng.NextDouble() * 4.5 + 2.0;
                var ellipse = new Ellipse
                {
                    Width = size,
                    Height = size,
                    Fill = i % 3 == 0
                        ? new SolidColorBrush(Color.FromArgb(190, 255, 183, 197))
                        : new SolidColorBrush(Color.FromArgb(180, 255, 255, 255))
                };

                var flake = new Snowflake
                {
                    Shape = ellipse,
                    X = _rng.NextDouble() * 600,
                    Y = _rng.NextDouble() * 600,
                    SpeedY = _rng.NextDouble() * 1.1 + 0.4,
                    DriftX = _rng.NextDouble() * 0.6 + 0.2,
                    Phase = _rng.NextDouble() * Math.PI * 2
                };

                Canvas.SetLeft(ellipse, flake.X);
                Canvas.SetTop(ellipse, flake.Y);
                SnowCanvas.Children.Add(ellipse);
                _snowflakes.Add(flake);
            }
        }

        private void UpdateSnowfall(object? sender, EventArgs e)
        {
            if (SnowCheck.IsChecked != true) return;

            foreach (var f in _snowflakes)
            {
                f.Y += f.SpeedY;
                f.Phase += 0.03;
                f.X += Math.Sin(f.Phase) * f.DriftX;

                if (f.Y > 600)
                {
                    f.Y = -10;
                    f.X = _rng.NextDouble() * 600;
                }

                Canvas.SetLeft(f.Shape, f.X);
                Canvas.SetTop(f.Shape, f.Y);
            }
        }

        private void SnowCheck_Changed(object sender, RoutedEventArgs e)
        {
            if (SnowCanvas != null)
                SnowCanvas.Visibility = SnowCheck.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        }

        private void LoadSettingsIntoUi()
        {
            // Brain
            EndpointBox.Text = _settings.ApiEndpoint;
            ApiKeyBox.Password = _settings.ApiKey;
            ModelBox.Text = _settings.ModelName;
            TelepathyCheck.IsChecked = _settings.EnableTelepathyContext;
            ProviderCombo.SelectedIndex = _settings.ProviderPreset switch
            {
                "Ollama (Local - Free)" => 0,
                "OpenRouter (Cloud)" => 1,
                "Google Gemini (OpenAI Compat)" => 2,
                "OpenAI (Official)" => 3,
                _ => 4
            };

            // Voice
            EnableVoiceCheck.IsChecked = _settings.EnableVoice;
            TtsEndpointBox.Text = _settings.TtsEndpoint;
            TtsKeyBox.Password = _settings.TtsApiKey;
            TtsVoiceIdBox.Text = _settings.TtsVoiceId;
            VolumeSlider.Value = _settings.VoiceVolume;
            TtsProviderCombo.SelectedIndex = _settings.TtsProvider switch
            {
                "Windows Built-in (Instant / Offline)" => 0,
                "Kokoro / Piper Local Server (OpenAI Compat)" => 1,
                "OpenAI TTS (Cloud)" => 2,
                _ => 3
            };

            // Personality
            PersonalityBox.Text = _settings.SystemPrompt;

            // Physics & Anya
            ScaleSlider.Value = _settings.SpriteScale;
            GravitySlider.Value = _settings.Gravity;
            MusicSlider.Value = _settings.MusicSensitivity;
            WalkCheck.IsChecked = _settings.AutonomousWalking;
            TopmostCheck.IsChecked = _settings.AlwaysOnTop;
            SnowCheck.IsChecked = _settings.EnableSnowInGui;

            FpsCombo.SelectedIndex = _settings.TargetFps switch
            {
                60  => 0,
                90  => 1,
                120 => 2,
                144 => 3,
                240 => 4,
                260 => 5,
                _   => 3
            };

            UpdateLabels();
        }

        private void ProviderCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isInitializing || ProviderCombo.SelectedItem is not ComboBoxItem item) return;
            switch (item.Content?.ToString())
            {
                case "Ollama (Local - Free)":
                    EndpointBox.Text = "http://localhost:11434/v1/chat/completions";
                    ApiKeyBox.Password = "ollama";
                    ModelBox.Text = "gemma3:4b";
                    break;
                case "OpenRouter (Cloud)":
                    EndpointBox.Text = "https://openrouter.ai/api/v1/chat/completions";
                    ModelBox.Text = "google/gemini-2.5-flash";
                    break;
                case "Google Gemini (OpenAI Compat)":
                    EndpointBox.Text = "https://generativelanguage.googleapis.com/v1beta/openai/chat/completions";
                    ModelBox.Text = "gemini-2.5-flash";
                    break;
                case "OpenAI (Official)":
                    EndpointBox.Text = "https://api.openai.com/v1/chat/completions";
                    ModelBox.Text = "gpt-4o-mini";
                    break;
            }
        }

        private void TtsProviderCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isInitializing || TtsProviderCombo.SelectedItem is not ComboBoxItem item) return;
            switch (item.Content?.ToString())
            {
                case "Kokoro / Piper Local Server (OpenAI Compat)":
                    TtsEndpointBox.Text = "http://localhost:8880/v1/audio/speech";
                    TtsVoiceIdBox.Text = "af_bella";
                    break;
                case "OpenAI TTS (Cloud)":
                    TtsEndpointBox.Text = "https://api.openai.com/v1/audio/speech";
                    TtsVoiceIdBox.Text = "nova";
                    break;
                case "Fish Audio / Custom HTTP Endpoint":
                    TtsEndpointBox.Text = "https://api.fish.audio/v1/tts";
                    break;
            }
        }

        private void Sliders_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!_isInitializing) UpdateLabels();
        }

        private void UpdateLabels()
        {
            if (ScaleLabel != null) ScaleLabel.Text = $"Anya Size (Scale): {(int)(ScaleSlider.Value * 100)}%";
            if (GravityLabel != null) GravityLabel.Text = $"Gravity Strength: {GravitySlider.Value:F2}";
            if (MusicLabel != null) MusicLabel.Text = $"Music Dance Sensitivity (Spotify / Music Apps): {MusicSlider.Value:F2}";
            if (VolumeLabel != null) VolumeLabel.Text = $"Voice Volume: {(int)(VolumeSlider.Value * 100)}%";
        }

        private void ResetPersonality_Click(object sender, RoutedEventArgs e)
        {
            PersonalityBox.Text = new AppSettings().SystemPrompt;
        }

        private async void TestBrain_Click(object sender, RoutedEventArgs e)
        {
            SyncUiToSettings();
            BrainStatusText.Text = "Connecting to HQ...";
            string reply = await _brain.AskAnyaAsync("Say 'Waku waku! Brain online!' in 5 words.");
            BrainStatusText.Text = reply;
        }

        private async void TestVoice_Click(object sender, RoutedEventArgs e)
        {
            SyncUiToSettings();
            await _voice.SpeakAsync("Waku waku! Agent Anya reporting for duty!", showErrors: true);
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            SyncUiToSettings();
            _settings.Save();
            _onSettingsApplied?.Invoke();
            SaveStatusText.Text = $"✨ Saved & Applied ({_settings.TargetFps} FPS)!";
        }

        private void SyncUiToSettings()
        {
            if (ProviderCombo.SelectedItem is ComboBoxItem bItem)
                _settings.ProviderPreset = bItem.Content?.ToString() ?? "Custom Endpoint";
            _settings.ApiEndpoint = EndpointBox.Text.Trim();
            _settings.ApiKey = string.IsNullOrWhiteSpace(ApiKeyBox.Password) ? "ollama" : ApiKeyBox.Password.Trim();
            _settings.ModelName = ModelBox.Text.Trim();
            _settings.EnableTelepathyContext = TelepathyCheck.IsChecked == true;

            if (TtsProviderCombo.SelectedItem is ComboBoxItem vItem)
                _settings.TtsProvider = vItem.Content?.ToString() ?? "Windows Built-in (Instant / Offline)";
            _settings.EnableVoice = EnableVoiceCheck.IsChecked == true;
            _settings.TtsEndpoint = TtsEndpointBox.Text.Trim();
            _settings.TtsApiKey = TtsKeyBox.Password.Trim();
            _settings.TtsVoiceId = TtsVoiceIdBox.Text.Trim();
            _settings.VoiceVolume = VolumeSlider.Value;

            _settings.SystemPrompt = PersonalityBox.Text;

            _settings.SpriteScale = Math.Round(ScaleSlider.Value, 2);
            _settings.Gravity = Math.Round(GravitySlider.Value, 2);
            _settings.MusicSensitivity = Math.Round(MusicSlider.Value, 2);
            _settings.AutonomousWalking = WalkCheck.IsChecked == true;
            _settings.AlwaysOnTop = TopmostCheck.IsChecked == true;
            _settings.EnableSnowInGui = SnowCheck.IsChecked == true;

            if (FpsCombo.SelectedItem is ComboBoxItem fpsItem && fpsItem.Content != null)
            {
                string fpsText = fpsItem.Content.ToString()!.Split(' ')[0];
                if (int.TryParse(fpsText, out int parsedFps))
                    _settings.TargetFps = parsedFps;
            }
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed) DragMove();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();
    }
}