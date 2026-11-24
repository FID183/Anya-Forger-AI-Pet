using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace DesktopCompanion
{
    public class Brain
    {
        private readonly AppSettings _settings;
        private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(25) };

        public Brain(AppSettings settings)
        {
            _settings = settings;
        }

        public async Task<string> AskAnyaAsync(string userMessage, List<string>? openWindowTitles = null)
        {
            try
            {
                string telepathyContext = (_settings.EnableTelepathyContext && openWindowTitles is { Count: > 0 })
                    ? $"\n[Telepathy / Active Windows on User's PC: {string.Join(", ", openWindowTitles)}]"
                    : "";

                var payload = new
                {
                    model = _settings.ModelName,
                    messages = new[]
                    {
                        new { role = "system", content = _settings.SystemPrompt + telepathyContext },
                        new { role = "user", content = userMessage }
                    },
                    max_tokens = 110,
                    temperature = 0.85
                };

                var request = new HttpRequestMessage(HttpMethod.Post, _settings.ApiEndpoint);
                request.Headers.Add("Authorization", $"Bearer {_settings.ApiKey}");
                request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

                var response = await _http.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                    return "Gaaah! Anya's brain is smoking! (Check Brain tab in Mainframe)";

                using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                return doc.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString()?.Trim() ?? "Waku waku!";
            }
            catch
            {
                return "Shock! No signal from HQ! (Open Anya Mainframe -> Brain to configure AI)";
            }
        }
    }
}