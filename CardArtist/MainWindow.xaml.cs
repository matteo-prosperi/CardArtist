using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CardArtist
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public static MainWindow Singleton { get; private set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public static Project? Project { get; private set; }

        public MainWindow()
        {
            Singleton = this;
            InitializeComponent();
        }

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
        }

        private void OnLoadProjectButtonClick(object sender, RoutedEventArgs e)
        {
            var dlg = new System.Windows.Forms.FolderBrowserDialog();
            if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                Project = new Project(dlg.SelectedPath);
                Project.Expand();
                ProjectTree.DataContext = Project;
                UpdateButton.IsEnabled = true;
                NewTemplateButton.IsEnabled = true;
                NewCardButton.IsEnabled = true;
            }
        }

        private async void OnNewTemplateButtonClick(object sender, RoutedEventArgs e)
        {
            var dlg = new SaveFileDialog
            {
                InitialDirectory = Project!.TemplatesFolderPath,
                DefaultExt = "*.razor",
                Filter = "Card template|*.razor"
            };
            if (dlg.ShowDialog() == true)
            {
                using var stream = dlg.OpenFile();
                using var writer = new StreamWriter(stream);
                await writer.WriteAsync(
@"@using System
<Grid xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
      xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
      Background=""White""
      Width=""2.75in""
      Height=""3.75in"">
    <Border x:Name=""Card""
            Width=""2.5in""
            Height=""3.5in""
            Margin=""0.125in""
            Padding=""0.125in""
            CornerRadius=""10""
            BorderBrush=""Black""
            BorderThickness=""1"">
        <Grid x:Name=""SafeArea"">
        </Grid>
    </Border>
</Grid>");
            }
        }

        private async void OnNewCardButtonClick(object sender, RoutedEventArgs e)
        {
            var dlg = new SaveFileDialog
            {
                InitialDirectory = Project!.CardsFolderPath,
                DefaultExt = "*.xml",
                Filter = "Card list|*.xml"
            };
            if (dlg.ShowDialog() == true)
            {
                using var stream = dlg.OpenFile();
                using var writer = new StreamWriter(stream);
                await writer.WriteAsync(
@"<Deck Template=""t1"" Dpi=""300"">
    <Card Id=""1""></Card>
    <Card Id=""2""></Card>
</Deck>");
            }
        }

        private void OnProjectTreeItemExpanded(object sender, RoutedEventArgs e)
        {
            ((e.OriginalSource as TreeViewItem)?.DataContext as ProjectFolder)?.Expand();
        }

        private void OnProjectTreeItemCollapsed(object sender, RoutedEventArgs e)
        {
            ((e.OriginalSource as TreeViewItem)?.DataContext as ProjectFolder)?.Collapse();
        }

        private void OnProjectTreeSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            var item = (ProjectItem)ProjectTree.SelectedItem;

            if (!(item is ProjectFolder))
            {
                switch (Path.GetExtension(item.FullPath).ToLower())
                {
                    case ".razor":
                    case ".xml":
                    case ".xaml":
                    case ".cs":
                        SetCurrentView(item.FullPath, null, null);
                        return;
                    case ".jpg":
                    case ".jpeg":
                    case ".png":
                        SetCurrentView(null, null, item.FullPath);
                        return;
                }
            }
            SetCurrentView(null, null, null);
        }

        private async void OnUpdateRenderButtonClick(object sender, RoutedEventArgs e)
        {
            try
            {
                IsEnabled = false;
                Mouse.OverrideCursor = Cursors.Wait;
                SetCurrentView(null, null, null);
                using var generator = new RendersGenerator(Project!, BorderCheck.IsChecked!.Value, CropCheck.IsChecked!.Value);
                await generator.GenerateAsync();
            }
            catch (Exception ex)
            {
                SetCurrentView(null, ex, null);
            }
            finally
            {
                IsEnabled = true;
                Mouse.OverrideCursor = null;
            }
        }

        private void SetCurrentView(string? textFilePath, Exception? exception, string? imagePath)
        {
            if (exception != null)
            {
                TextEditor.Text = exception.ToString();
                TextEditor.Background = new SolidColorBrush(Colors.PaleVioletRed);
                TextEditorScroll.Visibility = Visibility.Visible;
                CardRender.Source = null;
                CardRender.Visibility = Visibility.Collapsed;
            }
            else
            {
                try
                {
                    if (textFilePath != null)
                    {
                        TextEditor.Text = File.ReadAllText(textFilePath);
                        TextEditor.Background = new SolidColorBrush(Colors.White);
                        TextEditorScroll.Visibility = Visibility.Visible;
                        CardRender.Source = null;
                        CardRender.Visibility = Visibility.Collapsed;
                    }
                    else if (imagePath != null)
                    {
                        TextEditor.Text = "";
                        TextEditor.Background = new SolidColorBrush(Colors.White);
                        TextEditorScroll.Visibility = Visibility.Collapsed;
                        using var fileStream = new FileStream(imagePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                        CardRender.Source = BitmapFrame.Create(fileStream, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
                        CardRender.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        TextEditor.Text = "";
                        TextEditor.Background = new SolidColorBrush(Colors.White);
                        TextEditorScroll.Visibility = Visibility.Visible;
                        CardRender.Source = null;
                        CardRender.Visibility = Visibility.Collapsed;
                    }
                }
                catch (Exception ex)
                {
                    SetCurrentView(null, ex, null);
                }
            }
        }
    }
}
