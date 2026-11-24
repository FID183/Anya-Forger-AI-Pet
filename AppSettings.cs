using System;
using System.IO;
using System.Text.Json;

namespace DesktopCompanion
{
    public class AppSettings
    {
        // Единый экземпляр настроек для всего приложения (MainWindow + Mainframe + VoiceEngine)
        private static AppSettings? _current;
        public static AppSettings Current => _current ??= Load();

        // --- BRAIN TAB ---
        public string ProviderPreset { get; set; } = "Ollama (Local - Free)";
        public string ApiEndpoint { get; set; } = "http://localhost:11434/v1/chat/completions";
        public string ApiKey { get; set; } = "ollama";
        public string ModelName { get; set; } = "gemma3:4b";
        public bool EnableTelepathyContext { get; set; } = true;

        // --- VOICE TAB ---
        public bool EnableVoice { get; set; } = true;
        public string TtsProvider { get; set; } = "Windows Built-in (Instant / Offline)";
        public string TtsEndpoint { get; set; } = "https://api.fish.audio/v1/tts";
        public string TtsApiKey { get; set; } = "";
        public string TtsVoiceId { get; set; } = "af_bella";
        public double VoiceVolume { get; set; } = 0.85;

        // --- PERSONALITY TAB ---
        public string SystemPrompt { get; set; } =
            "You are Anya Forger from the anime 'Spy x Family', living directly on the user's Windows desktop in a Christmas outfit.\n" +
            "You are a telepath who can read the user's mind (see their open windows).\n" +
            "- Always respond in English.\n" +
            "- Keep responses super short and punchy (1-2 sentences max, under 130 characters).\n" +
            "- Use Anya's iconic phrases: 'Waku waku!', 'Heh.', 'Shock!', and mention peanuts or spy missions.";

        // --- ANYA & PHYSICS TAB ---
        public double SpriteScale { get; set; } = 1.0;
        public double Gravity { get; set; } = 0.85;
        public double MusicSensitivity { get; set; } = 0.04;
        public bool AutonomousWalking { get; set; } = true;
        public bool AlwaysOnTop { get; set; } = true;
        public bool EnableSnowInGui { get; set; } = true;
        public int TargetFps { get; set; } = 144; // По умолчанию 144 FPS (доступно: 60, 90, 120, 144, 240, 260)

        private static string ConfigPath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");

        public static AppSettings Load()
        {
            try
            {
                if (File.Exists(ConfigPath))
                {
                    string json = File.ReadAllText(ConfigPath);
                    _current = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                    return _current;
                }
            }
            catch { }
            _current = new AppSettings();
            return _current;
        }

        public void Save()
        {
            try
            {
                _current = this;
                var options = new JsonSerializerOptions { WriteIndented = true };
                File.WriteAllText(ConfigPath, JsonSerializer.Serialize(this, options));
            }
            catch { }
        }
    }
}