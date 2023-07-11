using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
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
    public class IsNullConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value == null);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new InvalidOperationException("IsNullConverter can only be used OneWay.");
        }
    }
    public class JigsawImageBoxConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return null;
            return (value as Menu.JigsawPuzzle).PreviewBoxLikeImage;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new InvalidOperationException("IsNullConverter can only be used OneWay.");
        }
    }
    public class JigsawImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return null;
            return (value as Menu.JigsawPuzzle).Image;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new InvalidOperationException("IsNullConverter can only be used OneWay.");
        }
    }

    public partial class Menu : UserControl
    {
        public MainWindow window = null;

        public Menu(MainWindow window)
        {
            InitializeComponent();
            this.window = window;
        }

        [Serializable]
        public class JigsawPuzzle
        {
            [NonSerialized]
            public BitmapSource PreviewBoxLikeImage;

            [NonSerialized]
            public BitmapSource Image;

            [NonSerialized]
            public BitmapSource BoxFaceImage;
            public MainWindow.JigsawPiece[,] Pieces { get; set; }

            public string Name { get; set; }
            public string Creator { get; set; }
            public int PiecesCount { get; set; }
            public string CentimetreSizeString { get; private set; }
            public int PiecePixelSize { get; set; }
            public int BackgroundIndex { get; set; }
            public DateTime CreationDate { get; set; }
            public DateTime StartDate { get; set; }
            public TimeSpan WorkingTime { get; set; }
            public int Percentage { get; set; }
            public int PiecesDone { get; set; }
            public bool Finished { get; set; }
            public int HorizontalPieces { get; set; }
            public int VerticalPieces { get; set; }

            const double CM_IN_PIXEL = 0.02636;
            Size jigsawSize;

            public Size JigsawSize
            {
                get { return jigsawSize; }
                set
                {
                    jigsawSize = value;
                    CentimetreSizeString = $"{(int)(value.Width * CM_IN_PIXEL)} x {(int)(value.Height * CM_IN_PIXEL)} см";
                }
            }
        }

        void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            foreach (UIElement item in categoriesPanel.Children.OfType<Border>())
                item.MouseLeftButtonUp += Item_MouseLeftButtonUp;

            for (int i = 0; i < 8; i++)
                list.Items.Add(null);

            Item_MouseLeftButtonUp(category1, null);
        }

        public void Item_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Border border = sender as Border;
            double y = border.TranslatePoint(new Point(), categoriesPanel).Y;

            var page = grid.Children.OfType<FrameworkElement>().FirstOrDefault(fd => fd.Name == "page");
            if (page != null) grid.Children.Remove(page);

            if (Convert.ToInt32(border.Name.Last().ToString()) < 6)
            {
                if (loadingImagesComplete)
                {
                    listGrid.Visibility = Visibility.Visible;
                    window.ScaleTransition(list.Parent as UIElement, 1.2);

                    if (border.Name == "category1")
                        LoadImagesFromServer(string.Empty);

                    if (border.Name == "category2")
                        LoadImagesFromServer("nature");

                    if (border.Name == "category3")
                        LoadImagesFromServer("city");

                    if (border.Name == "category4")
                        LoadImagesFromServer("vehicle");

                    if (border.Name == "category5")
                        LoadImagesFromServer("painting");
                }
            }
            else
            {
                listGrid.Visibility = Visibility.Collapsed;
                
                if (border.Name == "category6")
                    ClickCategory6();

                if (border.Name == "category7")
                    ClickCategory7();

                if (border.Name == "category8")
                    ClickCategory8();
            }

            foreach (Border item in categoriesPanel.Children.OfType<Border>())
            {
                ((item.Child as StackPanel).Children[0] as Rectangle).Fill =
                    item == border ? thumb.Fill : Brushes.Black;

                ((item.Child as StackPanel).Children[1] as TextBlock).Foreground =
                    item == border ? thumb.Fill : Brushes.Black;
            }

            thumb.BeginAnimation(MarginProperty, new ThicknessAnimation
            {
                To = new Thickness(0, categoriesPanel.Margin.Top + y, 0, 0),
                Duration = TimeSpan.FromSeconds(0.4),
                EasingFunction = new PowerEase { Power = 5 }
            });
        }

        void ClickCategory6()
        {
            if (grid.Children.OfType<JigsawCollectionView>().Any() == false)
            {
                JigsawCollectionView view = new JigsawCollectionView { Menu = this, Name = "page" };
                grid.Children.Add(view);
                Grid.SetColumn(view, 1);
                window.ScaleTransition(view, 1.2);
            }
        }

        void ClickCategory7()
        {
            if (grid.Children.OfType<JigsawFinishedCollectionView>().Any() == false)
            {
                JigsawFinishedCollectionView view = new JigsawFinishedCollectionView { Menu = this, Name = "page" };
                grid.Children.Add(view);
                Grid.SetColumn(view, 1);
                window.ScaleTransition(view, 1.2);
            }
        }

        void ClickCategory8()
        {
            if (grid.Children.OfType<LoadUserJigsawPage>().Any() == false)
            {
                LoadUserJigsawPage page = new LoadUserJigsawPage { Name = "page" };
                grid.Children.Add(page);
                Grid.SetColumn(page, 1);
                window.ScaleTransition(page, 1.2);
            }
        }

        void list_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (list.SelectedItem != null)
            {
                JigsawPuzzle jigsawPuzzle = list.SelectedItem as JigsawPuzzle;
                list.SelectedItem = null;
                ShowPuzzleDialog(jigsawPuzzle);
            }
        }

        public void ShowPuzzleDialog(JigsawPuzzle jigsaw)
        {
            PuzzleInfo puzzleInfo = new PuzzleInfo(jigsaw);
            Rectangle rectangle = puzzleInfo.CreateBackground();

            puzzleInfo.Unloaded += async (s, a) =>
            {
                if (puzzleInfo.DialogResult == PuzzleInfo.PuzzleInfoDialogResult.StartJigsaw)
                    StartJigsaw(puzzleInfo.JigsawPuzzle);
                else if (puzzleInfo.DialogResult == PuzzleInfo.PuzzleInfoDialogResult.Remove)
                    RemoveJigsawFromCollection(puzzleInfo.JigsawPuzzle);
                else if (puzzleInfo.DialogResult == PuzzleInfo.PuzzleInfoDialogResult.AddToCollection)
                {
                    await Task.Delay(100);
                    AddToCollection(puzzleInfo.JigsawPuzzle);
                }
            };

            Grid.SetColumnSpan(rectangle, 2);
            grid.Children.Add(rectangle);

            Grid.SetColumnSpan(puzzleInfo, 2);
            grid.Children.Add(puzzleInfo);
        }

        public async void StartJigsaw(JigsawPuzzle jigsaw)
        {
            if (jigsaw != null)
            {
                if (MainWindow.jigsaws.Contains(jigsaw) == false)
                    MainWindow.jigsaws.Add(jigsaw);

                window.StartLoadingScreen();
                Grid grid = Parent as Grid;
                if (grid != null) grid.Children.Remove(this);

                await Task.Delay(50);
                window.PrepareWorkspace(jigsaw);
            }
        }

        public static void AddToCollection(JigsawPuzzle puzzle)
        {
            MainWindow.jigsaws.Add(puzzle);
            DataInputOutput.SaveJigsawImagesToFileSystem(puzzle);
            DataInputOutput.SaveJigsawDataToFileSystem(puzzle);
            new WindowCornerNotification("Пазл был добавлен в коллекцию");
        }

        void RemoveJigsawFromCollection(JigsawPuzzle jigsaw)
        {
            var view1 = grid.Children.OfType<JigsawCollectionView>().FirstOrDefault();
            var view2 = grid.Children.OfType<JigsawFinishedCollectionView>().FirstOrDefault();

            if (view1 != null)
            {
                if (view1.list.Items.Contains(jigsaw))
                {
                    view1.list.Items.Remove(jigsaw);
                    view1.UpdateCountLabel();
                }
            }

            if (view2 != null)
            {
                if (view2.list.Items.Contains(jigsaw))
                {
                    view2.list.Items.Remove(jigsaw);
                    view2.UpdateCountLabel();
                }
            }

            MainWindow.jigsaws.Remove(jigsaw);
            bool removed = DataInputOutput.RemoveJigsawFromFileSystem(jigsaw);

            new WindowCornerNotification(removed ? "Пазл удалён из коллекции" :
                "Не удалось полностью удалить пазл", !removed);
        }

        bool loadingAnimation = false;
        string lastFilter = string.Empty;
        int pieceSize = 90;

        bool LIC = true;

        public bool loadingImagesComplete
        {
            get { return LIC; }
            set
            {
                LIC = value;

                foreach (FrameworkElement item in categoriesPanel.Children)
                {
                    if (item.Name.StartsWith("category") && int.Parse(item.Name.Last().ToString()) <= 5)
                        item.IsEnabled = value;
                }
            }
        }

        async void LoadImagesFromServer(string filter)
        {
            list.Visibility = Visibility.Visible;
            errorSign.Visibility = Visibility.Collapsed;

            for (int i = 0; i < 8; i++)
                list.Items[i] = null;

            if (loadingAnimation == false)
            {
                SetRectenglesLoadingAnimation(true);
                loadingAnimation = true;
            }

            lastFilter = filter;
            loadingImagesComplete = false;

            Unsplasharp.UnsplasharpClient client = new Unsplasharp.UnsplasharpClient("AQrBtEthIgOa3UK8SifneTJYPzHz7an58x9L9GHri7g");
            var photos = await client.GetRandomPhoto(8, query: filter);

            if (photos.Count == 0)
            {
                ShowLoadImagesError();
                return;
            }

            for (int i = 0; i < photos.Count; i++)
            {
                int index = i;
                BitmapImage image;

                using (var webClient = new System.Net.WebClient())
                {
                    try
                    {
                        byte[] imageBytes = await webClient.DownloadDataTaskAsync(photos[index].Urls.Regular);
                        image = ToImage(imageBytes);
                    }
                    catch
                    {
                        ShowLoadImagesError();
                        return;
                    }

                    if (loadingAnimation)
                    {
                        loadingAnimation = false;
                        loadingImagesComplete = true;
                        SetRectenglesLoadingAnimation(false);
                    }

                    Unsplasharp.Models.Photo photo = photos[index];
                    string user = string.IsNullOrWhiteSpace(photo.User.Name) ? "Unsplash user" : photo.User.Name;

                    string name = string.IsNullOrWhiteSpace(photo.Description) ?
                        (string.IsNullOrWhiteSpace(photo.Location.Name) ? "Photo by " + user : photo.Location.Name) : photo.Description;

                    bool result = DateTime.TryParse(photo.CreatedAt, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime date);
                    if (result == false) date = DateTime.Now;

                    list.Items[index] = CreateJigsawPuzzle(image, name, user, pieceSize, date);
                }
            }

            BitmapImage ToImage(byte[] array)
            {
                using (var ms = new System.IO.MemoryStream(array))
                {
                    var image = new BitmapImage();
                    image.BeginInit();
                    image.CacheOption = BitmapCacheOption.OnLoad;
                    image.StreamSource = ms;
                    image.EndInit();
                    return image;
                }
            }
            void ShowLoadImagesError()
            {
                list.Visibility = Visibility.Collapsed;
                errorSign.Visibility = Visibility.Visible;
                SetRectenglesLoadingAnimation(false);
                loadingImagesComplete = true;
                loadingAnimation = false;
            }
        }

        public static JigsawPuzzle CreateJigsawPuzzle(BitmapSource image, string name, string creator, int pieceSize, DateTime date)
        {
            var data = GetPiecesCount(image.PixelWidth, image.PixelHeight, pieceSize);
            BitmapSource bitmap = CreateResizedImage(image, data.newImageSize.Width, data.newImageSize.Height);

            JigsawPuzzle jigsawPuzzle = new JigsawPuzzle
            {
                Name = name,
                Creator = creator,
                Image = bitmap,
                PiecesCount = data.count,
                PiecePixelSize = pieceSize,
                JigsawSize = data.jigsawSize,
                BackgroundIndex = MainWindow.random.Next(0, 3),
                CreationDate = date,
                HorizontalPieces = data.wPieces,
                VerticalPieces = data.hPieces
            };

            jigsawPuzzle.BoxFaceImage = PackageGraphics.CreateJigsawPackingBoxFace(jigsawPuzzle, bitmap.PixelWidth, bitmap.PixelHeight);
            var previewSize = Resize(data.newImageSize.Width, data.newImageSize.Height, 400, 400);
            jigsawPuzzle.PreviewBoxLikeImage = PackageGraphics.CreateJigsawPackageBox(jigsawPuzzle.BoxFaceImage, previewSize.Width, previewSize.Height);

            return jigsawPuzzle;
        }

        public static (Size jigsawSize, Size newImageSize, int wPieces, int hPieces, int count) GetPiecesCount(double width, double height, int pieceSize)
        {
            var newImageSize = Resize(width, height, 1280, 720);
            int wPieces = (int)(newImageSize.Width / pieceSize);
            int hPieces = (int)(newImageSize.Height / pieceSize);

            Size jigsawSize = new Size(wPieces * pieceSize, hPieces * pieceSize);
            int piecesCount = (int)(jigsawSize.Width / pieceSize * jigsawSize.Height / pieceSize);

            return (jigsawSize, newImageSize, wPieces, hPieces, piecesCount);    
        }

        void SetRectenglesLoadingAnimation(bool loading)
        {
            foreach (var item in loadingRectengles)
            {
                item.BeginAnimation(OpacityProperty, !loading ? null : new DoubleAnimation
                {
                    From = 0,
                    To = 0.2,
                    Duration = TimeSpan.FromSeconds(0.5),
                    AutoReverse = true,
                    RepeatBehavior = RepeatBehavior.Forever
                });

                item.Visibility = loading ? Visibility.Visible
                    : Visibility.Collapsed;
            }
        }

        static Size Resize(double originalWidth, double originalHeight, double maxWidth, double maxHeight)
        {
            double widthRatio = originalWidth / maxWidth;
            double heightRatio = originalHeight / maxHeight;
            double resizeRatio = Math.Max(widthRatio, heightRatio);

            if (resizeRatio <= 1.0)
                return new Size(originalWidth, originalHeight);
            else
            {
                double newWidth = originalWidth / resizeRatio;
                double newHeight = originalHeight / resizeRatio;

                return new Size(newWidth, newHeight);
            }
        }
        static BitmapFrame CreateResizedImage(ImageSource source, double width, double height)
        {
            var rect = new Rect(0, 0, width - 0 * 2, height - 0 * 2);
            var group = new DrawingGroup();

            RenderOptions.SetBitmapScalingMode(group, BitmapScalingMode.HighQuality);
            group.Children.Add(new ImageDrawing(source, rect));
            var drawingVisual = new DrawingVisual();

            using (var drawingContext = drawingVisual.RenderOpen())
                drawingContext.DrawDrawing(group);

            var resizedImage = new RenderTargetBitmap((int)width, (int)height, 96, 96, PixelFormats.Default);
            resizedImage.Render(drawingVisual);

            return BitmapFrame.Create(resizedImage);
        }

        void category8_DragEnter(object sender, DragEventArgs e)
        {
            if (grid.Children.OfType<LoadUserJigsawPage>().Any() == false)
                Item_MouseLeftButtonUp(category8, null);
        }

        void category8_Drop(object sender, DragEventArgs e)
        {
            LoadUserJigsawPage page = grid.Children.OfType<LoadUserJigsawPage>().FirstOrDefault();
            if (page != null) page.Rectangle_Drop(null, e);
        }

        void Border_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (loadingImagesComplete)
                LoadImagesFromServer(lastFilter);
        }

        void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (window != null)
            {
                ComboBox comboBox = sender as ComboBox;
                pieceSize = Convert.ToInt32(((ComboBoxItem)comboBox.SelectedItem).Tag);

                if (loadingImagesComplete)
                    LoadImagesFromServer(lastFilter);
            }
        }

        List<Rectangle> loadingRectengles = new List<Rectangle>();

        void Rectangle_Initialized(object sender, EventArgs e)
        {
            loadingRectengles.Add(sender as Rectangle);

            if (loadingRectengles.Count == 8)
            {
                SetRectenglesLoadingAnimation(true);
                loadingAnimation = true;
            }
        }

        void TextBlock_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            try { Process.Start("https://github.com/QRFiling"); }
            catch { }
        }

        void TextBlock_MouseLeftButtonUp_1(object sender, MouseButtonEventArgs e)
        {
            try { Process.Start("https://unsplash.com/developers"); }
            catch { }
        }
    }
}
