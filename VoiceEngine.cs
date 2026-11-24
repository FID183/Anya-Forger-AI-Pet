using System;
using System.IO;
using System.Net.Http;
using System.Speech.Synthesis;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using NAudio.Wave;

namespace DesktopCompanion
{
    public class VoiceEngine
    {
        private readonly AppSettings _settings;
        private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(25) };
        private readonly SpeechSynthesizer _synth = new();

        public VoiceEngine(AppSettings settings)
        {
            _settings = settings;
            try
            {
                _synth.SelectVoiceByHints(VoiceGender.Female, VoiceAge.Child);
            }
            catch { }
        }

        public async Task SpeakAsync(string text, bool showErrors = false)
        {
            if (!_settings.EnableVoice || string.IsNullOrWhiteSpace(text))
                return;

            if (_settings.TtsProvider.StartsWith("Windows Built-in"))
            {
                await Task.Run(() =>
                {
                    try
                    {
                        _synth.Volume = (int)Math.Clamp(_settings.VoiceVolume * 100, 0, 100);
                        _synth.Rate = 1;
                        _synth.Speak(text);
                    }
                    catch { }
                });
                return;
            }

            try
            {
                string endpoint = _settings.TtsEndpoint.Trim();
                bool isFishAudio = endpoint.Contains("fish.audio", StringComparison.OrdinalIgnoreCase)
                                   || _settings.TtsProvider.Contains("Fish Audio", StringComparison.OrdinalIgnoreCase);

                string jsonPayload;
                if (isFishAudio)
                {
                    // Нативный формат Fish Audio API (/v1/tts)
                    var fishPayload = new
                    {
                        text = text,
                        reference_id = _settings.TtsVoiceId.Trim(),
                        format = "mp3",
                        mp3_bitrate = 128,
                        latency = "normal"
                    };
                    jsonPayload = JsonSerializer.Serialize(fishPayload);
                }
                else
                {
                    // Стандартный формат OpenAI / Kokoro FastAPI
                    var openAiPayload = new
                    {
                        model = "tts-1",
                        input = text,
                        voice = string.IsNullOrWhiteSpace(_settings.TtsVoiceId) ? "af_bella" : _settings.TtsVoiceId.Trim(),
                        response_format = "mp3"
                    };
                    jsonPayload = JsonSerializer.Serialize(openAiPayload);
                }

                var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
                if (!string.IsNullOrWhiteSpace(_settings.TtsApiKey))
                    request.Headers.Add("Authorization", $"Bearer {_settings.TtsApiKey.Trim()}");

                if (isFishAudio)
                    request.Headers.Add("model", "s1"); // Актуальная модель Fish Audio

                request.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                var response = await _http.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    if (showErrors)
                    {
                        string errBody = await response.Content.ReadAsStringAsync();
                        MessageBox.Show($"Fish Audio / TTS Error ({(int)response.StatusCode}):\n{errBody}", "Voice Engine Error");
                    }
                    return;
                }

                byte[] audioBytes = await response.Content.ReadAsByteArrayAsync();
                await PlayMp3BytesAsync(audioBytes);
            }
            catch (Exception ex)
            {
                if (showErrors)
                    MessageBox.Show($"TTS Connection Failed:\n{ex.Message}", "Voice Engine Error");
            }
        }

        private async Task PlayMp3BytesAsync(byte[] mp3Bytes)
        {
            await Task.Run(async () =>
            {
                using var ms = new MemoryStream(mp3Bytes);
                using var reader = new Mp3FileReader(ms);
                using var waveOut = new WaveOutEvent { Volume = (float)Math.Clamp(_settings.VoiceVolume, 0.0, 1.0) };
                waveOut.Init(reader);
                waveOut.Play();
                while (waveOut.PlaybackState == PlaybackState.Playing)
                    await Task.Delay(50);
            });
        }
    }
}