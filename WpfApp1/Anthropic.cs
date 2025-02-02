using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace WpfApp1
{
    public class AnthropicService
    {
        private static readonly HttpClient client = new HttpClient();
        private readonly string _apiKey;
        private readonly List<ChatMessage> _conversationHistory = new List<ChatMessage>();

        private class ChatMessage
        {
            public string Role { get; set; }
            public string Content { get; set; }
        }

        public AnthropicService(string apiKey)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
                throw new ArgumentException("API key cannot be null or empty.");
            _apiKey = apiKey;
        }

        public void ClearConversation()
        {
            _conversationHistory.Clear();
        }

        public async Task<string> GetAnthropicResponse(string text, string model)
        {
            if (string.IsNullOrWhiteSpace(text))
                return "Error: Input text cannot be empty.";

            _conversationHistory.Add(new ChatMessage { Role = "user", Content = text });

            var messageList = new List<object>();
            foreach (var msg in _conversationHistory)
            {
                messageList.Add(new { role = msg.Role, content = msg.Content });
            }

            var requestUri = "https://api.anthropic.com/v1/messages";
            var requestBody = new
            {
                model = "claude-3-5-sonnet-20241022",
                messages = messageList,
                max_tokens = 1024
            };

            var jsonRequestBody = JsonConvert.SerializeObject(requestBody);
            var content = new StringContent(jsonRequestBody, Encoding.UTF8, "application/json");

            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("x-api-key", _apiKey);
            client.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");

            try
            {
                var response = await client.PostAsync(requestUri, content);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    var errorResponse = JsonConvert.DeserializeObject<dynamic>(responseBody);
                    return $"Error: API request failed with status code {response.StatusCode}. Message: {errorResponse?.error?.message ?? responseBody}";
                }

                var jsonResponse = JsonConvert.DeserializeObject<AnthropicResponse>(responseBody);
                if (jsonResponse == null || jsonResponse.Content == null || jsonResponse.Content.Count == 0)
                    return "Error: Unexpected response format or empty completion.";

                var assistantReply = jsonResponse.Content[0].Text;
                _conversationHistory.Add(new ChatMessage { Role = "assistant", Content = assistantReply });

                return assistantReply;
            }
            catch (HttpRequestException e)
            {
                return $"Error: Network issue encountered. {e.Message}";
            }
            catch (TaskCanceledException e)
            {
                return $"Error: Request timed out. {e.Message}";
            }
            catch (JsonException e)
            {
                return $"Error: Failed to parse API response. {e.Message}";
            }
            catch (Exception e)
            {
                return $"Error: An unexpected error occurred. {e.Message}";
            }
        }

        private class AnthropicResponse
        {
            [JsonProperty("content")]
            public List<AnthropicContent> Content { get; set; }
        }

        private class AnthropicContent
        {
            [JsonProperty("type")]
            public string Type { get; set; }

            [JsonProperty("text")]
            public string Text { get; set; }
        }
    }
}
