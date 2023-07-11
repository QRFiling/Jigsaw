using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace Jigsaw
{
    public partial class PuzzleInfo : UserControl
    {
        public enum PuzzleInfoDialogResult
        {
            None,
            AddToCollection,
            StartJigsaw,
            Remove
        }

        public Menu.JigsawPuzzle JigsawPuzzle { get; set; }
        PuzzleInfoDialogResult dialogResult;
        Rectangle background;

        public PuzzleInfoDialogResult DialogResult
        {
            get { return dialogResult; }
            private set
            {
                dialogResult = value;
                Close();
            }
        }

        public PuzzleInfo(Menu.JigsawPuzzle jigsaw)
        {
            InitializeComponent();

            JigsawPuzzle = jigsaw;
            image.Source = jigsaw.Image;
            box.Source = jigsaw.PreviewBoxLikeImage;
            title.Text = jigsaw.Name;
            tabel1.Text = jigsaw.PiecesCount + " шт";
            table2.Text = jigsaw.Creator;
            table3.Text = jigsaw.CentimetreSizeString;
            table4.Text = $"{jigsaw.Image.PixelWidth}x{jigsaw.Image.PixelHeight} пт";
            table5.Text = jigsaw.CreationDate.ToString("d MMMM yyyy");

            if (MainWindow.jigsaws.Contains(jigsaw))
            {
                Grid grid = leftButton.Parent as Grid;
                grid.ColumnDefinitions[1].Width = new GridLength(0);
                grid.ColumnDefinitions[2].Width = new GridLength(0);

                (leftButton.Child as TextBlock).Text = "Удалить";
                (leftButton.Child as TextBlock).Foreground = Brushes.LightCoral;
            }

            imageTranslate.BeginAnimation(TranslateTransform.XProperty, new DoubleAnimation
            {
                From = Width,
                To = 0,
                Duration = TimeSpan.FromSeconds(2.5),
                EasingFunction = new PowerEase { Power = 5, EasingMode = EasingMode.EaseInOut },
                RepeatBehavior = RepeatBehavior.Forever,
                AutoReverse = true
            });

            boxTranslate.BeginAnimation(TranslateTransform.XProperty, new DoubleAnimation
            {
                From = 0,
                To = -Width,
                Duration = TimeSpan.FromSeconds(2.5),
                EasingFunction = new PowerEase { Power = 5, EasingMode = EasingMode.EaseInOut },
                RepeatBehavior = RepeatBehavior.Forever,
                AutoReverse = true
            });
        }

        public Rectangle CreateBackground()
        {
            background = new Rectangle { Fill = new SolidColorBrush(Colors.Black) { Opacity = 0.5 } };
            background.MouseLeftButtonUp += Border_MouseLeftButtonUp_1;

            return background;
        }

        void Border_MouseDown(object sender, MouseButtonEventArgs e) => DialogResult = PuzzleInfoDialogResult.StartJigsaw;
        void Border_MouseLeftButtonUp_1(object sender, MouseButtonEventArgs e) => DialogResult = PuzzleInfoDialogResult.None;

        void Border_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            DialogResult = (leftButton.Child as TextBlock).Text == "Удалить" ? PuzzleInfoDialogResult.Remove :
                PuzzleInfoDialogResult.AddToCollection;
        }

        void Close()
        {
            DoubleAnimation animation = new DoubleAnimation
            {
                To = 0.7,
                Duration = TimeSpan.FromSeconds(0.25),
                EasingFunction = new PowerEase { Power = 3, EasingMode = EasingMode.EaseIn }
            };

            animation.Completed += (s, e) =>
            {
                (Parent as Panel).Children.Remove(background);
                (Parent as Panel).Children.Remove(this);
            };

            RenderTransform.BeginAnimation(ScaleTransform.ScaleXProperty, animation);
        }
    }
}
