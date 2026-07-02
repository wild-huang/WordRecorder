using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace WordRecorder.Services;

public class AiService
{
    private HttpClient? _httpClient;
    private string _apiEndpoint = "";
    private string _apiKey = "";
    private string _model = "";
    private CancellationTokenSource? _cts;

    public void Configure(string endpoint, string apiKey, string model)
    {
        _apiEndpoint = endpoint.TrimEnd('/');
        _apiKey = apiKey;
        _model = model;
        _httpClient?.Dispose();
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
    }

    public async Task<List<string>> SuggestWordsAsync(string input, int maxSuggestions = 5)
    {
        if (_httpClient == null || string.IsNullOrEmpty(_apiEndpoint) || string.IsNullOrEmpty(_apiKey))
            return new List<string>();

        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        var token = _cts.Token;

        try
        {
            var prompt = $"The user is typing an English word: \"{input}\". Suggest up to {maxSuggestions} possible complete English words that start with these letters. Return ONLY a JSON array of strings, nothing else. Example: [\"hello\", \"help\", \"held\"]";

            var requestBody = new
            {
                model = _model,
                messages = new[]
                {
                    new { role = "system", content = "You are a word suggestion assistant. Return only valid JSON arrays." },
                    new { role = "user", content = prompt }
                },
                temperature = 0.3,
                max_tokens = 200
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_apiEndpoint}/chat/completions", content, token);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync(token);
            using var doc = JsonDocument.Parse(responseJson);

            var messageContent = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString() ?? "[]";

            // Try to parse the JSON array from the response
            messageContent = messageContent.Trim();
            if (messageContent.StartsWith("```"))
            {
                // Remove markdown code blocks
                var lines = messageContent.Split('\n');
                var sb = new StringBuilder();
                foreach (var line in lines)
                {
                    if (!line.StartsWith("```"))
                        sb.AppendLine(line);
                }
                messageContent = sb.ToString().Trim();
            }

            var suggestions = JsonSerializer.Deserialize<List<string>>(messageContent);
            return suggestions ?? new List<string>();
        }
        catch (OperationCanceledException)
        {
            return new List<string>();
        }
        catch
        {
            return new List<string>();
        }
    }
}
