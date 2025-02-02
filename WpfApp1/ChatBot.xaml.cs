using DotNetEnv;
using Markdig.Wpf;
using MaterialDesignColors;
using MaterialDesignThemes.Wpf;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace WpfApp1
{
    public partial class ChatBot: Window
    {
        private readonly OpenAiService openAiService;
        private readonly MistralService mistralService;
        private readonly AnthropicService anthropicService;
        private bool IsDarkTheme = true;

        public ChatBot()
        {
            InitializeComponent();
            Env.Load();

            string openAiApiKey = Env.GetString("OPENAI_API_KEY");
            if (string.IsNullOrEmpty(openAiApiKey))
            {
                MessageBox.Show("Missing OpenAI API Key...", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            openAiService = new OpenAiService(openAiApiKey);

            string anthropicApiKey = Env.GetString("ANTHROPIC_API_KEY");
            if (string.IsNullOrEmpty(anthropicApiKey))
            {
                MessageBox.Show("Missing Anthropic API Key...", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            anthropicService = new AnthropicService(anthropicApiKey);

            string mistralApiKey = Env.GetString("MISTRAL_API_KEY");
            if (string.IsNullOrEmpty(mistralApiKey))
            {
                MessageBox.Show("Missing Mistral API Key...", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            mistralService = new MistralService(mistralApiKey);

            OverwriteDarkTheme();
        }

        private async void StoredInputText(object sender, RoutedEventArgs e)
        {
            string text = TextInput.Text;
            if (!string.IsNullOrEmpty(text))
            {
                TextInput.Text = string.Empty;
                TextOutput.Document.Blocks.Add(new Paragraph(new Run("User: " + text)));

                string selectedModel = (ModelSelector.SelectedItem as ComboBoxItem)?.Content.ToString();

                TextOutput.Document.Blocks.Add(new Paragraph(new Run(selectedModel + ": [Thinking...]")));
                TextOutput.ScrollToEnd();

                string aiResponse = string.Empty;

                if (selectedModel.StartsWith("o") || selectedModel.StartsWith("g"))
                {
                    aiResponse = await openAiService.GetOpenAiResponse(text, selectedModel);
                }
                else if (selectedModel.StartsWith("a"))
                {
                    aiResponse = await anthropicService.GetAnthropicResponse(text, selectedModel);
                }
                else if (selectedModel.StartsWith("m"))
                {
                    aiResponse = await mistralService.GetMistralResponse(text, selectedModel);
                }

                TextOutput.Document.Blocks.Remove(TextOutput.Document.Blocks.LastBlock);

                if (!string.IsNullOrEmpty(aiResponse))
                {
                    var markdownViewer = new MarkdownViewer { Markdown = aiResponse };
                    var doc = markdownViewer.Document;
                    int i = 0;
                    foreach (var block in doc.Blocks.ToList())
                    {
                        if (i++ == 0)
                        {
                            var textRange = new TextRange(block.ContentStart, block.ContentEnd);
                            TextOutput.Document.Blocks.Add(new Paragraph(new Run(selectedModel + ": " + textRange.Text)));
                        }
                        else
                        {
                            TextOutput.Document.Blocks.Add(block);
                        }
                    }
                }
                else
                {
                    TextOutput.Document.Blocks.Add(new Paragraph(new Run("AI: [No response or error]")));
                }
                TextOutput.ScrollToEnd();
            }
        }

        private void ThemeToggle_Click(object sender, RoutedEventArgs e)
        {
            if (IsDarkTheme)
            {
                OverwriteLightTheme();
                ThemeToggle.Content = "Light";
            }
            else
            {
                OverwriteDarkTheme();
                ThemeToggle.Content = "Dark";
            }
            IsDarkTheme = !IsDarkTheme;
        }

        private void OverwriteDarkTheme()
        {
            var paletteHelper = new PaletteHelper();
            var darkTheme = Theme.Create(
                baseTheme: Theme.Dark,
                primary: SwatchHelper.Lookup[MaterialDesignColor.Orange],
                accent: SwatchHelper.Lookup[MaterialDesignColor.OrangeSecondary]
            );
            paletteHelper.SetTheme(darkTheme);
        }

        private void OverwriteLightTheme()
        {
            var paletteHelper = new PaletteHelper();
            var lightTheme = Theme.Create(
                baseTheme: Theme.Light,
                primary: SwatchHelper.Lookup[MaterialDesignColor.Orange],
                accent: SwatchHelper.Lookup[MaterialDesignColor.OrangeSecondary]
            );
            paletteHelper.SetTheme(lightTheme);
        }
        private void ReturnButton_Click(object sender, RoutedEventArgs e)
        {
            MainWindow mainWindow = new MainWindow();
            mainWindow.Show();
            this.Close();
        }

    }
}
