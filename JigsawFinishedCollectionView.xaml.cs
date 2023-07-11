using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Jigsaw
{
    public class TimeToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return null;
            TimeSpan time = (TimeSpan)value;

            if (time.TotalMinutes < 1)
                return (int)time.TotalSeconds + " сек";
            else if (time.TotalHours < 1)
                return (int)time.TotalMinutes + " мин";
            else if (time.TotalDays < 1)
                return (int)time.TotalHours + " ч";
            else
                return Math.Round(time.TotalDays, 1) + " дн";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new InvalidOperationException("IsNullConverter can only be used OneWay.");
        }
    }

    public partial class JigsawFinishedCollectionView : UserControl
    {
        public Menu Menu { get; set; }

        public JigsawFinishedCollectionView()
        {
            InitializeComponent();
        }

        void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            foreach (var item in MainWindow.jigsaws)
            {
                if (item.Finished)
                    list.Items.Add(item);
            }

            UpdateCountLabel();
        }

        public void UpdateCountLabel()
        {
            jigsawCount.Text = $"{list.Items.Count} " +
                $"{JigsawCollectionView.Declination(list.Items.Count, "пазл", "пазла", "пазлов")}";
        }

        void list_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (list.SelectedItem != null)
            {
                if (borders.Any(a => a.IsMouseOver))
                {
                    list.SelectedItem = null;
                    return;
                }

                BitmapSource bitmap = (list.SelectedItem as Menu.JigsawPuzzle).Image;
                list.SelectedItem = null;

                Rectangle rectangle = new Rectangle
                {
                    Fill = new SolidColorBrush(Colors.White)
                    { Opacity = 0.85 }
                };

                Image image = new Image
                {
                    MaxWidth = bitmap.PixelWidth,
                    MaxHeight = bitmap.PixelHeight,
                    Margin = new Thickness(100),
                    Source = bitmap,
                    Clip = new RectangleGeometry { RadiusX = 10, RadiusY = 10 },
                };

                image.SizeChanged += (s, a) =>
                {
                    (image.Clip as RectangleGeometry).Rect =
                        new Rect(0, 0, image.ActualWidth, image.ActualHeight);
                };

                image.MouseLeftButtonUp += Image_MouseLeftButtonUp;
                rectangle.MouseLeftButtonUp += Image_MouseLeftButtonUp;

                void Image_MouseLeftButtonUp(object s, MouseButtonEventArgs a)
                {
                    DoubleAnimation animation = new DoubleAnimation
                    {
                        To = 0.3,
                        Duration = TimeSpan.FromSeconds(0.3),
                        EasingFunction = new PowerEase { Power = 5, EasingMode = EasingMode.EaseIn }
                    };

                    DoubleAnimation animation2 = new DoubleAnimation
                    {
                        To = animation.To,
                        Duration = animation.Duration,
                        EasingFunction = animation.EasingFunction
                    };

                    animation.Completed += (p, i) =>
                    {
                        Menu.grid.Children.Remove(rectangle);
                        Menu.grid.Children.Remove(image);
                    };

                    image.RenderTransform.BeginAnimation(ScaleTransform.ScaleXProperty, animation);
                    image.RenderTransform.BeginAnimation(ScaleTransform.ScaleYProperty, animation2);
                }

                Grid.SetColumnSpan(rectangle, 2);
                Menu.grid.Children.Add(rectangle);

                Grid.SetColumnSpan(image, 2);
                Menu.grid.Children.Add(image);

                Menu.window.ScaleTransition(image, 0.65);
            }
        }

        void list_PreviewMouseWheel(object sender, MouseWheelEventArgs e) =>
            e.Handled = true;

        void Border_MouseLeftButtonUp(object sender, MouseButtonEventArgs e) =>
            Menu.ShowPuzzleDialog((sender as FrameworkElement).DataContext as Menu.JigsawPuzzle);

        List<Border> borders = new List<Border>();
        void Border_Loaded(object sender, RoutedEventArgs e) => borders.Add(sender as Border);

        void Border_MouseLeftButtonUp_1(object sender, MouseButtonEventArgs e)
        {
            Menu.JigsawPuzzle jigsaw = (sender as FrameworkElement).DataContext as Menu.JigsawPuzzle;

            jigsaw.Pieces = null;
            jigsaw.Finished = false;
            jigsaw.StartDate = DateTime.MinValue;
            jigsaw.WorkingTime = TimeSpan.Zero;
            jigsaw.Percentage = 0;
            jigsaw.PiecesDone = 0;

            DataInputOutput.SaveJigsawDataToFileSystem(jigsaw);
            Menu.Item_MouseLeftButtonUp(Menu.category6, null);
            new WindowCornerNotification("Пазл был разобран!");
        }
    }
}
