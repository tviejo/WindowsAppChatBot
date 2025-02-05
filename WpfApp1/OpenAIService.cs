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
    public class OpenAiService
    {
        private static readonly HttpClient client = new HttpClient();
        private readonly string _apiKey;

        public OpenAiService(string apiKey)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
                throw new ArgumentException("API key cannot be null or empty.");
            _apiKey = apiKey;
        }
        /// Envoie la conversation complète à OpenAI.
        /// </summary>

        /// <summary>
        public async Task<string> GetOpenAiResponse(List<ConversationMessage> conversationHistory, string model)
        {
            if (conversationHistory == null || conversationHistory.Count == 0)
            {
                return "Error: Conversation history is empty.";
            }

            var messages = conversationHistory
                .Select(m => new { role = m.Role.ToLowerInvariant(), content = m.Content })
                .ToList();

            var requestUri = "https://api.openai.com/v1/chat/completions";

            object requestBody;

            if (model.StartsWith("o"))
            {
                requestBody = new
                {
                    model = model,
                    messages = messages,
                    max_completion_tokens = 500,
                    temperature = 1
                };
            }
            else
            {
                requestBody = new
                {
                    model = model,
                    messages = messages,
                    max_tokens = 500,
                    temperature = 0.7
                };
            }

            var jsonRequestBody = JsonConvert.SerializeObject(requestBody);
            var httpContent = new StringContent(jsonRequestBody, Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(HttpMethod.Post, requestUri)
            {
                Content = httpContent
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

            try
            {
                var response = await client.SendAsync(request);
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
