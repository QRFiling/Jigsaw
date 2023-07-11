using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Linq;
using System.Collections.Generic;

namespace Jigsaw
{
    public partial class WindowCornerNotification : UserControl
    {
        Grid grid = Application.Current.MainWindow.Content as Grid;

        public WindowCornerNotification(string text, bool fail = false)
        {
            InitializeComponent();
            List<WindowCornerNotification> list = grid.Children.OfType<WindowCornerNotification>().ToList();

            foreach (var item in list)
                grid.Children.Remove(item);

            textControl.Text = text;
            grid.Children.Add(this);
            Panel.SetZIndex(this, 10);
            UpdateLayout();

            if (fail)
            {
                (Content as Border).Background = new SolidColorBrush(Color.FromRgb(60, 60, 60));
                MainWindow.cornerNofificationFailPlayer.Position = TimeSpan.Zero;
                MainWindow.cornerNofificationFailPlayer.Play();
            }
            else
            {
                MainWindow.cornerNofificationPlayer.Position = TimeSpan.Zero;
                MainWindow.cornerNofificationPlayer.Play();
            }

            translate.BeginAnimation(TranslateTransform.XProperty, new DoubleAnimation
            {
                From = -(ActualWidth + Margin.Left),
                To = 0,
                Duration = TimeSpan.FromSeconds(1),
                EasingFunction = new PowerEase { Power = 10 }
            });
        }

        async void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            await Task.Delay(2000);

            DoubleAnimation animation = new DoubleAnimation
            {
                To = -(ActualWidth + Margin.Left),
                Duration = TimeSpan.FromSeconds(1),
                EasingFunction = new PowerEase { Power = 10, EasingMode = EasingMode.EaseIn }
            };

            animation.Completed += (s, a) => grid.Children.Remove(this);
            translate.BeginAnimation(TranslateTransform.XProperty, animation);
        }
    }
}
