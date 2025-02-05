using DotNetEnv;
using Markdig.Wpf;
using MaterialDesignColors;
using MaterialDesignThemes.Wpf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;

namespace WpfApp1
{
    public partial class ChatComparisonWindow : Window
    {
        private readonly OpenAiService openAiService;
        private readonly MistralService mistralService;
        private readonly AnthropicService anthropicService;
        private bool IsDarkTheme = true;

        private class ChatColumn
        {
            public ComboBox ModelSelector { get; set; }
            public RichTextBox ChatOutput { get; set; }
            public List<ConversationMessage> ConversationHistory { get; set; }
        }

        private List<ChatColumn> chatColumns = new List<ChatColumn>();

        public ChatComparisonWindow()
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

        protected override void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);
            GenerateChatColumns(1);
        }

        private UniformGrid ChatColumnsPanelControl
        {
            get { return this.FindName("ChatColumnsPanel") as UniformGrid; }
        }

        private void ModelCountSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ModelCountSelector.SelectedItem is ComboBoxItem item &&
                int.TryParse(item.Content.ToString(), out int count))
            {
                GenerateChatColumns(count);
            }
        }

        private void GenerateChatColumns(int count)
        {
            var panel = ChatColumnsPanelControl;
            if (panel == null)
            {
                return;
            }
            panel.Children.Clear();
            chatColumns.Clear();
            panel.Columns = count;
            for (int i = 0; i < count; i++)
            {
                Grid columnGrid = new Grid();
                columnGrid.Margin = new Thickness(5);
                columnGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                columnGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                ComboBox modelSelector = new ComboBox
                {
                    Height = 30,
                    Margin = new Thickness(0, 0, 0, 10),
                    Padding = new Thickness(5),
                    FontSize = 14,
                    BorderThickness = new Thickness(1),
                    Style = (Style)FindResource("MaterialDesignOutlinedComboBox")
                };
                modelSelector.SetResourceReference(Control.ForegroundProperty, "MaterialDesignBody");
                modelSelector.SetResourceReference(Control.BackgroundProperty, "MaterialDesignBackground");
                modelSelector.SetResourceReference(Control.BorderBrushProperty, "PrimaryHueMidBrush");
                modelSelector.Items.Add(new ComboBoxItem { Content = "--- OpenAI ---", IsEnabled = false });
                modelSelector.Items.Add(new ComboBoxItem { Content = "gpt-4o-mini" });
                modelSelector.Items.Add(new ComboBoxItem { Content = "gpt-4o" });
                modelSelector.Items.Add(new ComboBoxItem { Content = "o1-mini" });
                modelSelector.Items.Add(new ComboBoxItem { Content = "gpt-3.5-turbo" });
                modelSelector.Items.Add(new ComboBoxItem { Content = "--- Mistral ---", IsEnabled = false });
                modelSelector.Items.Add(new ComboBoxItem { Content = "mistral-tiny" });
                modelSelector.Items.Add(new ComboBoxItem { Content = "mistral-small-latest" });
                modelSelector.Items.Add(new ComboBoxItem { Content = "mistral-medium-latest" });
                modelSelector.Items.Add(new ComboBoxItem { Content = "mistral-large-latest" });;
                modelSelector.Items.Add(new ComboBoxItem { Content = "--- Anthropic ---", IsEnabled = false });
                modelSelector.Items.Add(new ComboBoxItem { Content = "claude-3-5-sonnet-20241022" });
                modelSelector.Items.Add(new ComboBoxItem { Content = "claude-3-opus-20240229" });
                modelSelector.Items.Add(new ComboBoxItem { Content = "claude-3-haiku-20240307" });
                modelSelector.SelectedIndex = 1;
                Grid.SetRow(modelSelector, 0);
                columnGrid.Children.Add(modelSelector);
                ScrollViewer scrollViewer = new ScrollViewer
                {
                    VerticalScrollBarVisibility = ScrollBarVisibility.Visible,
                    Padding = new Thickness(0),
                    Margin = new Thickness(0, 0, 0, 20),
                    BorderThickness = new Thickness(1)
                };
                scrollViewer.SetResourceReference(Control.BackgroundProperty, "MaterialDesignCardBackground");
                scrollViewer.SetResourceReference(Control.BorderBrushProperty, "PrimaryHueMidBrush");
                Border border = new Border
                {
                    CornerRadius = new CornerRadius(15),
                    Padding = new Thickness(0)
                };
                border.SetResourceReference(Border.BackgroundProperty, "MaterialDesignCardBackground");
                RichTextBox chatOutput = new RichTextBox
                {
                    IsReadOnly = true,
                    BorderThickness = new Thickness(0),
                    Padding = new Thickness(15),
                    FontSize = 16
                };
                chatOutput.SetResourceReference(Control.ForegroundProperty, "MaterialDesignBody");
                chatOutput.SetResourceReference(Control.BackgroundProperty, "MaterialDesignCardBackground");
                FlowDocument doc = new FlowDocument();
                Paragraph welcomePara = new Paragraph(new Run($"Welcome to Windows AI Chat – Model {i + 1}"));
                doc.Blocks.Add(welcomePara);
                chatOutput.Document = doc;
                border.Child = chatOutput;
                scrollViewer.Content = border;
                Grid.SetRow(scrollViewer, 1);
                columnGrid.Children.Add(scrollViewer);
                panel.Children.Add(columnGrid);
                chatColumns.Add(new ChatColumn
                {
                    ModelSelector = modelSelector,
                    ChatOutput = chatOutput,
                    ConversationHistory = new List<ConversationMessage>()
                });
            }
        }

        private async void SendButton_Click(object sender, RoutedEventArgs e)
        {
            string text = TextInput.Text;
            if (!string.IsNullOrWhiteSpace(text))
            {
                TextInput.Text = string.Empty;
                foreach (var column in chatColumns)
                {
                    column.ChatOutput.Document.Blocks.Add(new Paragraph(new Run("User: " + text)));
                    column.ChatOutput.ScrollToEnd();
                    column.ConversationHistory.Add(new ConversationMessage { Role = "user", Content = text });
                }
                List<Task> tasks = new List<Task>();
                foreach (var column in chatColumns)
                {
                    string selectedModel = (column.ModelSelector.SelectedItem as ComboBoxItem)?.Content.ToString();
                    if (string.IsNullOrEmpty(selectedModel))
                        continue;
                    Paragraph thinkingPara = new Paragraph(new Run(selectedModel + ": [Thinking...]"));
                    column.ChatOutput.Document.Blocks.Add(thinkingPara);
                    column.ChatOutput.ScrollToEnd();
                    tasks.Add(Task.Run(async () =>
                    {
                        string aiResponse = string.Empty;
                        if (selectedModel.StartsWith("o") || selectedModel.StartsWith("gpt"))
                        {
                            aiResponse = await openAiService.GetOpenAiResponse(column.ConversationHistory, selectedModel);
                        }
                        else if (selectedModel.StartsWith("m"))
                        {
                            aiResponse = await mistralService.GetMistralResponse(column.ConversationHistory, selectedModel);
                        }
                        else if (selectedModel.StartsWith("c") || selectedModel.StartsWith("a"))
                        {
                            aiResponse = await anthropicService.GetAnthropicResponse(column.ConversationHistory, selectedModel);
                        }
                        Dispatcher.Invoke(() =>
                        {
                            column.ChatOutput.Document.Blocks.Remove(thinkingPara);
                            DisplayResponse(selectedModel, aiResponse, column.ChatOutput);
                            column.ChatOutput.ScrollToEnd();
                            column.ConversationHistory.Add(new ConversationMessage { Role = "assistant", Content = aiResponse });
                        });
                    }));
                }
                await Task.WhenAll(tasks);
            }
        }

        private void DisplayResponse(string modelName, string aiResponse, RichTextBox target)
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

        private void ThemeToggle_Click(object sender, RoutedEventArgs e)
        {
            if (IsDarkTheme)
            {
                OverwriteLightTheme();
            }
            else
            {
                OverwriteDarkTheme();
            }
            IsDarkTheme = !IsDarkTheme;
        }

        private void OverwriteDarkTheme()
        {
            var paletteHelper = new PaletteHelper();
            var darkTheme = Theme.Create(Theme.Dark, SwatchHelper.Lookup[MaterialDesignColor.Blue], SwatchHelper.Lookup[MaterialDesignColor.BlueSecondary]);
            paletteHelper.SetTheme(darkTheme);
        }

        private void OverwriteLightTheme()
        {
            var paletteHelper = new PaletteHelper();
            var lightTheme = Theme.Create(Theme.Light, SwatchHelper.Lookup[MaterialDesignColor.Blue], SwatchHelper.Lookup[MaterialDesignColor.BlueSecondary]);
            paletteHelper.SetTheme(lightTheme);
        }

        private void ResetChat_Click(object sender, RoutedEventArgs e)
        {
            ResetChat();
        }

        private void ResetChat()
        {
            for (int i = 0; i < chatColumns.Count; i++)
            {
                var column = chatColumns[i];
                column.ChatOutput.Document.Blocks.Clear();
                Paragraph welcomePara = new Paragraph(new Run($"Welcome to Windows AI Chat – Model {i + 1}"));
                column.ChatOutput.Document.Blocks.Add(welcomePara);
                column.ChatOutput.ScrollToEnd();
                column.ConversationHistory.Clear();
            }
        }
    }
}
