using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace WpfApp1
{
    public class MistralService
    {
        private static readonly HttpClient client = new HttpClient();
        private readonly string _apiKey;

        private readonly List<ChatMessage> _conversationHistory = new List<ChatMessage>();

        public MistralService(string apiKey)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new ArgumentException("API key cannot be null or empty.");
            }
            _apiKey = apiKey;
        }

        private class ChatMessage
        {
            public string Role { get; set; }
            public string Content { get; set; }
        }

        public void ClearConversation()
        {
            _conversationHistory.Clear();
        }

        public async Task<string> GetMistralResponse(string text, string model)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return "Error: Input text cannot be empty.";
            }

            _conversationHistory.Add(new ChatMessage { Role = "user", Content = text });

            var sbPrompt = new StringBuilder();
            foreach (var msg in _conversationHistory)
            {
                sbPrompt.AppendLine($"{msg.Role}: {msg.Content}");
            }

            var requestUri = "https://api.mistralai.com/v1/generate";

            var requestBody = new
            {
                model = model,           
                prompt = sbPrompt.ToString(),
                max_tokens = 500,        
                temperature = 0.7
            };

            var jsonRequestBody = JsonConvert.SerializeObject(requestBody);
            var content = new StringContent(jsonRequestBody, Encoding.UTF8, "application/json");

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

            try
            {
                var response = await client.PostAsync(requestUri, content);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    dynamic errorResponse = JsonConvert.DeserializeObject(responseBody);
                    return $"Error: API request failed with status code {response.StatusCode}. " +
                           $"Message: {errorResponse?.error ?? responseBody}";
                }

                dynamic jsonResponse = JsonConvert.DeserializeObject(responseBody);
                string mistralReply = jsonResponse?.completion;

                if (string.IsNullOrEmpty(mistralReply))
                {
                    return "Error: Unexpected response format or empty completion.";
                }

                _conversationHistory.Add(new ChatMessage { Role = "assistant", Content = mistralReply });

                return mistralReply;
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
    }
}
