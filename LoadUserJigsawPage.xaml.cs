using Microsoft.Win32;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace Jigsaw
{
    public partial class LoadUserJigsawPage : UserControl
    {
        public LoadUserJigsawPage()
        {
            InitializeComponent();
        }

        void Rectangle_DragEnter(object sender, DragEventArgs e) => rect.Opacity = 0.8;
        void TextBlock_DragEnter(object sender, DragEventArgs e) => Rectangle_DragEnter(null, null);
        void Rectangle_DragLeave(object sender, DragEventArgs e) => rect.Opacity = 0.3;
        void TextBlock_DragLeave(object sender, DragEventArgs e) => Rectangle_DragLeave(null, null);
        void TextBlock_Drop(object sender, DragEventArgs e) => Rectangle_Drop(null, null);

        public void Rectangle_Drop(object sender, DragEventArgs e)
        {
            rect.Opacity = 0.3;

            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                OpenUserImage(((string[])e.Data.GetData(DataFormats.FileDrop))[0]);
        }

        async void OpenUserImage(string file)
        {
            if (System.IO.File.Exists(file) == false)
                return;

            if (fileInfo.Visibility == Visibility.Visible)
            {
                fileInfo.Visibility = Visibility.Collapsed;
                await Task.Delay(100);
            }

            try
            {
                BitmapSource image = DataInputOutput.OpenImage(file);

                if (image.PixelWidth >= 400 && image.PixelHeight >= 400)
                {
                    fileImage.Source = image;
                    fileName.Text = System.IO.Path.GetFileNameWithoutExtension(file);
                    filePath.Text = file;
                    fileResolution.Text = $"{image.PixelWidth} x {image.PixelHeight} пт";
                    fileSize.Text = DataInputOutput.GetFileSizeString(file);

                    fileInfo.Visibility = Visibility.Visible;
                    slider.Value = -120;
                }
                else new WindowCornerNotification("Изображение слишком маленькое", true);
            }
            catch { new WindowCornerNotification("Не удалось открыть изображение", true); }
        }

        void TextBlock_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource == link)
            {
                OpenFileDialog dialog = new OpenFileDialog();
                bool? result = dialog.ShowDialog();

                if (result.HasValue && result.Value)
                    OpenUserImage(dialog.FileName);
            }
        }

        void Border_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Menu.JigsawPuzzle jigsaw = Menu.CreateJigsawPuzzle(fileImage.Source as BitmapSource,
                fileName.Text, Environment.UserName, pieceSize, DateTime.Now);

            Menu.AddToCollection(jigsaw);
            fileInfo.Visibility = Visibility.Collapsed;
        }

        int pieceSize = 120;

        void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (fileInfo.Visibility == Visibility.Visible)
            {
                pieceSize = (int)Math.Abs(e.NewValue);
                BitmapSource bitmap = fileImage.Source as BitmapSource;

                var pieces = Menu.GetPiecesCount(bitmap.PixelWidth, bitmap.PixelHeight, pieceSize);
                buttonTextCount.Text = pieces.count.ToString();
            }
        }
    }
}
