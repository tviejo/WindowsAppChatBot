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
    public partial class Comparaison : Window
    {
        private readonly OpenAiService openAiService;
        private readonly MistralService mistralService;
        private readonly AnthropicService anthropicService;
        private bool IsDarkTheme = true;

        public Comparaison()
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
                TextOutput1.Document.Blocks.Add(new Paragraph(new Run("User: " + text)));
                TextOutput2.Document.Blocks.Add(new Paragraph(new Run("User: " + text)));

                string selectedModel1 = (ModelSelector1.SelectedItem as ComboBoxItem)?.Content.ToString();
                string selectedModel2 = (ModelSelector2.SelectedItem as ComboBoxItem)?.Content.ToString();

                Paragraph thinkingPara1 = new Paragraph(new Run(selectedModel1 + ": [Thinking...]"));
                Paragraph thinkingPara2 = new Paragraph(new Run(selectedModel2 + ": [Thinking...]"));
                TextOutput1.Document.Blocks.Add(thinkingPara1);
                TextOutput2.Document.Blocks.Add(thinkingPara2);
                TextOutput1.ScrollToEnd();
                TextOutput2.ScrollToEnd();

                string aiResponse1 = string.Empty;
                string aiResponse2 = string.Empty;

                if (selectedModel1.StartsWith("o") || selectedModel1.StartsWith("gpt") )
                {
                    aiResponse1 = await openAiService.GetOpenAiResponse(text, selectedModel1);
                }
                else if (selectedModel1.StartsWith("m"))
                {
                    aiResponse1 = await mistralService.GetMistralResponse(text, selectedModel1);
                }
                else if (selectedModel1.StartsWith("c"))
                {
                    aiResponse1 = await anthropicService.GetAnthropicResponse(text, selectedModel1);
                }

                if (selectedModel2.StartsWith("o") || selectedModel2.StartsWith("gpt"))
                {
                    aiResponse2 = await openAiService.GetOpenAiResponse(text, selectedModel2);
                }
                else if (selectedModel2.StartsWith("m"))
                {
                    aiResponse2 = await mistralService.GetMistralResponse(text, selectedModel2);
                }
                else if (selectedModel2.StartsWith("c"))
                {
                    aiResponse2 = await anthropicService.GetAnthropicResponse(text, selectedModel2);
                }

                TextOutput1.Document.Blocks.Remove(thinkingPara1);
                TextOutput2.Document.Blocks.Remove(thinkingPara2);

                void DisplayResponse(string modelName, string aiResponse, RichTextBox target)
                {
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
                                target.Document.Blocks.Add(new Paragraph(new Run(modelName + ": " + textRange.Text)));
                            }
                            else
                            {
                                target.Document.Blocks.Add(block);
                            }
                        }
                    }
                    else
                    {
                        target.Document.Blocks.Add(new Paragraph(new Run(modelName + ": [No response or error]")));
                    }
                }

                DisplayResponse(selectedModel1, aiResponse1, TextOutput1);
                DisplayResponse(selectedModel2, aiResponse2, TextOutput2);
                TextOutput1.ScrollToEnd();
                TextOutput2.ScrollToEnd();
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
