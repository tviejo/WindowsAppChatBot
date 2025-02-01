using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace WpfApp1
{
    public partial class MainWindow : Window
    {
        private static readonly HttpClient client = new HttpClient();

        public MainWindow()
        {
            InitializeComponent();
        }

        private async void StoredInputText(object sender, RoutedEventArgs e)
        {
            //clean input text
            string text = TextInput.Text;
            if (!string.IsNullOrEmpty(text))
            {
                TextInput.Text = null;
                textOutput.Text += text + "\n";
                string aiResponse = await GetAiResponse(text);
                textOutput.Text += aiResponse + "\n";
            }
        }

        private async Task<string> GetAiResponse(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }

            var apiKey = "YOUR_OPENAI_API_KEY";
            var requestUri = "https://api.openai.com/v1/engines/davinci-codex/completions";

            var requestBody = new
            {
                prompt = text,
                max_tokens = 50
            };

            var jsonRequestBody = Newtonsoft.Json.JsonConvert.SerializeObject(requestBody);
            var content = new StringContent(jsonRequestBody, Encoding.UTF8, "application/json");

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            var response = await client.PostAsync(requestUri, content);
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync();
            dynamic jsonResponse = Newtonsoft.Json.JsonConvert.DeserializeObject(responseBody);

            return jsonResponse.choices[0].text;
        }
    }
}
