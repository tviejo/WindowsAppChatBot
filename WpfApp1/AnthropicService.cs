using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace WpfApp1
{
    // A simple conversation message model.

    public class AnthropicService
    {
        private static readonly HttpClient client = new HttpClient();
        private readonly string _apiKey;

        public AnthropicService(string apiKey)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
                throw new ArgumentException("API key cannot be null or empty.");
            _apiKey = apiKey;
        }

        public async Task<string> GetAnthropicResponse(List<ConversationMessage> conversationHistory, string model)
        {
            if (conversationHistory == null || conversationHistory.Count == 0)
            {
                return "Error: Conversation history is empty.";
            }

            // Convert the conversation history into the format expected by the API.
            var messages = new List<object>();
            foreach (var msg in conversationHistory)
            {
                // Optionally convert role to lowercase if required by the API.
                messages.Add(new { role = msg.Role.ToLowerInvariant(), content = msg.Content });
            }

            var requestUri = "https://api.anthropic.com/v1/messages";
            // Build the request body. You can adjust max_tokens, temperature, etc.
            var requestBody = new
            {
                model = model,
                messages = messages,
                max_tokens = 1024,
                temperature = 0.7
            };

            var jsonRequestBody = JsonConvert.SerializeObject(requestBody);
            var httpContent = new StringContent(jsonRequestBody, Encoding.UTF8, "application/json");

            // Clear any existing headers and set the ones needed for Anthropics.
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("x-api-key", _apiKey);
            client.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");

            try
            {
                var response = await client.PostAsync(requestUri, httpContent);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    dynamic errorResponse = JsonConvert.DeserializeObject(responseBody);
                    return $"Error: API request failed with status code {response.StatusCode}. Message: {errorResponse?.error?.message ?? responseBody}";
                }

                var jsonResponse = JsonConvert.DeserializeObject<AnthropicResponse>(responseBody);
                if (jsonResponse == null || jsonResponse.Content == null || jsonResponse.Content.Count == 0)
                    return "Error: Unexpected response format or empty completion.";

                // Return the text from the first message in the content list.
                return jsonResponse.Content[0].Text;
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

        // Internal classes to map the Anthropich API response.
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
