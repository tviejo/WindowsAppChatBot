using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

public class OpenAiService
{
    private static readonly HttpClient client = new HttpClient();
    private readonly string _apiKey;

    public OpenAiService(string apiKey)
    {
        _apiKey = apiKey;
    }

    public async Task<string> GetAiResponse(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        var requestUri = "https://api.openai.com/v1/chat/completions";

        // Prepare the request body for the Chat Completions API
        var requestBody = new
        {
            model = "gpt-3.5-turbo",
            messages = new[]
            {
                new {
                    role = "user",
                    content = text
                }
            },
            max_tokens = 50,
            temperature = 0.7
        };

        // Serialize the payload
        var jsonRequestBody = JsonConvert.SerializeObject(requestBody);
        var content = new StringContent(jsonRequestBody, Encoding.UTF8, "application/json");

        // Add the API key to the Authorization header
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

        // Send POST request
        var response = await client.PostAsync(requestUri, content);
        response.EnsureSuccessStatusCode();

        // Read and parse the response
        var responseBody = await response.Content.ReadAsStringAsync();
        dynamic jsonResponse = JsonConvert.DeserializeObject(responseBody);

        // Return the assistant's message content
        return (string)jsonResponse.choices[0].message.content;
    }
}
