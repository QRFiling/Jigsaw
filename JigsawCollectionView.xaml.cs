using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Linq;

namespace Jigsaw
{
    public class JigsawPercentStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null)
            {
                Menu.JigsawPuzzle jigsaw = value as Menu.JigsawPuzzle;
                if (jigsaw.Pieces == null) return null;
                return jigsaw.Percentage + "%";
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new InvalidOperationException("IsNullConverter can only be used OneWay.");
        }
    }
    public class JigsawPiecesDoneStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null)
            {
                Menu.JigsawPuzzle jigsaw = value as Menu.JigsawPuzzle;
                if (jigsaw.Pieces == null) return null;
                return $"({jigsaw.PiecesDone} / {jigsaw.Pieces.Length} шт готово)";
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new InvalidOperationException("IsNullConverter can only be used OneWay.");
        }
    }
    public class JigsawExtraInfoConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null)
            {
                Menu.JigsawPuzzle jigsaw = value as Menu.JigsawPuzzle;
                if (jigsaw.StartDate == DateTime.MinValue) return "Пазл ещё не был открыт";

                string time = new TimeToStringConverter().Convert(DateTime.Now - jigsaw.StartDate, null, null, null).ToString();
                return "Начат " + time + " назад";
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new InvalidOperationException("IsNullConverter can only be used OneWay.");
        }
    }

    public partial class JigsawCollectionView : UserControl
    {
        public Menu Menu { get; set; }

        public JigsawCollectionView()
        {
            InitializeComponent();
        }

        void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            foreach (var item in MainWindow.jigsaws)
            {
                if (item.Finished == false)
                    list.Items.Add(item);
            }

            UpdateCountLabel();
        }

        public void UpdateCountLabel()
        {
            jigsawCount.Text = $"{list.Items.Count} " +
                $"{Declination(list.Items.Count, "пазл", "пазла", "пазлов")}";
        }

        public static string Declination(int number, string nominativ, string genetiv, string plural)
        {
            var titles = new[] { nominativ, genetiv, plural };
            var cases = new[] { 2, 0, 1, 1, 1, 2 };
            return titles[number % 100 > 4 && number % 100 < 20 ? 2 : cases[(number % 10 < 5) ? number % 10 : 5]];
        }

        List<Border> borderButtons = new List<Border>();
        void Border_Loaded(object sender, RoutedEventArgs e) => borderButtons.Add(sender as Border);

        void list_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (list.SelectedItem != null)
            {
                if (borderButtons.First(f => f.DataContext == list.SelectedItem).IsMouseOver == false)
                    Menu.StartJigsaw(list.SelectedItem as Menu.JigsawPuzzle);

                list.SelectedItem = null;
            }
        }

        void Border_PreviewMouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e) =>
            Menu.ShowPuzzleDialog((sender as FrameworkElement).DataContext as Menu.JigsawPuzzle);

        void list_PreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e) =>
            e.Handled = true;
    }
}
