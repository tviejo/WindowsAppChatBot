using System;
using System.Collections.Generic;
using System.Linq;
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

        public MistralService(string apiKey)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
                throw new ArgumentException("API key cannot be null or empty.");
            _apiKey = apiKey;
        }

        public async Task<string> GetMistralResponse(List<ConversationMessage> conversationHistory, string model)
        {
            if (conversationHistory == null || conversationHistory.Count == 0)
                return "Error: Conversation history is empty.";

            var messages = conversationHistory
                .Select(msg => new { role = msg.Role, content = msg.Content })
                .ToList();

            var requestBody = new
            {
                model = model,
                messages = messages,
                max_tokens = 500,
                temperature = 0.7
            };

            var jsonRequestBody = JsonConvert.SerializeObject(requestBody);
            var httpContent = new StringContent(jsonRequestBody, Encoding.UTF8, "application/json");

            var requestUri = "https://api.mistral.ai/v1/chat/completions";

            using (var requestMessage = new HttpRequestMessage(HttpMethod.Post, requestUri)
            {
                Content = httpContent
            })
            {
                requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

                try
                {
                    var response = await client.SendAsync(requestMessage);
                    var responseBody = await response.Content.ReadAsStringAsync();

                    if (!response.IsSuccessStatusCode)
                    {
                        dynamic errorResponse = JsonConvert.DeserializeObject(responseBody);
                        return $"Error: API request failed with status code {response.StatusCode}. " +
                               $"Message: {errorResponse?.detail ?? responseBody}";
                    }

                    dynamic jsonResponse = JsonConvert.DeserializeObject(responseBody);
                    string mistralReply = jsonResponse?.choices[0]?.message?.content;

                    if (string.IsNullOrEmpty(mistralReply))
                    {
                        return "Error: Unexpected response format or empty completion.";
                    }

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
}
