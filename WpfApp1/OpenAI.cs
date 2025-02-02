using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace WpfApp1
{
    public class OpenAiService
    {
        private static readonly HttpClient client = new HttpClient();
        private readonly string _apiKey;

        private readonly List<ChatMessage> _conversationHistory = new List<ChatMessage>();

        public OpenAiService(string apiKey)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new ArgumentException("API key cannot be null or empty.");
            }
            _apiKey = apiKey;
        }

        private class ChatMessage
        {
            public string role { get; set; }
            public string content { get; set; }
        }

        public void ClearConversation()
        {
            _conversationHistory.Clear();
        }

        public async Task<string> GetOpenAiResponse(string text, string model)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return "Error: Input text cannot be empty.";
            }

            _conversationHistory.Add(new ChatMessage { role = "user", content = text });

            var requestUri = "https://api.openai.com/v1/chat/completions";

            object requestBody;

            if (model.StartsWith("o"))
            {
                requestBody = new
                {
                    model = model,
                    messages = _conversationHistory,
                    max_completion_tokens = 500,
                    temperature = 1
                };
            }
            else
            {
                requestBody = new
                {
                    model = model,
                    messages = _conversationHistory,
                    max_tokens = 500,
                    temperature = 0.7
                };
            }

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
                    return $"Error: API request failed with status code {response.StatusCode}. Response: {errorResponse?.error?.message}";
                }

                dynamic jsonResponse = JsonConvert.DeserializeObject(responseBody);
                string assistantReply = jsonResponse?.choices?[0]?.message?.content;

                if (string.IsNullOrEmpty(assistantReply))
                {
                    return "Error: Unexpected response format.";
                }

                _conversationHistory.Add(new ChatMessage { role = "assistant", content = assistantReply });

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
    }
}
