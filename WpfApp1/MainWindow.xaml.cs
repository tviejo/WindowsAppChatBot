using MaterialDesignColors;
using MaterialDesignThemes.Wpf;
using System;
using System.Windows;

namespace WpfApp1
{
    public partial class MainWindow : Window
    {
        private bool IsDarkTheme = true;

        public MainWindow()
        {
            InitializeComponent();
            OverwriteDarkTheme();
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

        private void ComparisonsButton_Click(object sender, RoutedEventArgs e)
        {
            Comparaison comparisonsWindow = new Comparaison();
            comparisonsWindow.Show();
            this.Close();
        }

        private void ChatbotButton_Click(object sender, RoutedEventArgs e)
        {
            ChatBot chatBotWindow = new ChatBot();
            chatBotWindow.Show();
            this.Close();
        }
    }
}
