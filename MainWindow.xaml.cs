using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Jigsaw
{
    public partial class MainWindow : Window
    {
        public static MediaPlayer cornerNofificationPlayer = new MediaPlayer();
        public static MediaPlayer cornerNofificationFailPlayer = new MediaPlayer();
        public static MediaPlayer boxOnTablePlayer = new MediaPlayer();
        public static MediaPlayer boxOpenPlayer = new MediaPlayer { Volume = 0.5 };
        public static MediaPlayer jigsawPiecePlayer = new MediaPlayer();

        public static List<Menu.JigsawPuzzle> jigsaws = new List<Menu.JigsawPuzzle>();
        public static Random random = new Random();
        JigsawPiece[,] pieces = null;

        public enum SideType
        {
            Active,
            Passive,
            Flat
        }

        [Serializable]
        public class JigsawPiece
        {
            public SideType LeftSide { get; set; }
            public SideType TopSide { get; set; }
            public SideType RightSide { get; set; }
            public SideType BottomSide { get; set; }

            public double Size { get; set; }
            public int IndexX { get; set; }
            public int IndexY { get; set; }

            public List<JigsawPiece> Neighbours { get; set; } = new List<JigsawPiece>();

            [NonSerialized]
            public Path VisualElement;
            public double SideSize { get; set; }
            public Point Location { get; set; }
        }

        public MainWindow()
        {
            InitializeComponent();
            PackageGraphics.DPI = VisualTreeHelper.GetDpi(this).PixelsPerDip;
            Icon = new BitmapImage(new Uri("pack://application:,,,/Jigsaw;component/Resources/Icon.ico"));

            jigsaws.AddRange(DataInputOutput.LoadJigsawsFromFileSystem());
            Border_MouseLeftButtonUp_3(null, null);

            DataInputOutput.InitializePlayer(cornerNofificationPlayer, "NotificationSound");
            DataInputOutput.InitializePlayer(cornerNofificationFailPlayer, "NotificationFailSound");
            DataInputOutput.InitializePlayer(boxOnTablePlayer, "BoxOnTableSound");
            DataInputOutput.InitializePlayer(boxOpenPlayer, "BoxOpenSound");
            DataInputOutput.InitializePlayer(jigsawPiecePlayer, "PuzzlePieceSound");

            ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls |
                SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;

            Application.Current.DispatcherUnhandledException += Current_DispatcherUnhandledException;
        }

        void Current_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            new WindowCornerNotification($"Необработаная ошибка: {e.Exception.Message}", true);
            e.Handled = true;
        }

        protected override void OnContentRendered(EventArgs e)
        {
            App.splashScreen.Close(TimeSpan.Zero);
            base.OnContentRendered(e);
        }

        public void StartLoadingScreen()
        {
            loadingScreen.Visibility = Visibility.Visible;
            loadingCircleRotate.BeginAnimation(RotateTransform.AngleProperty, new DoubleAnimation
            {
                From = 0,
                To = 360,
                Duration = TimeSpan.FromSeconds(0.5),
                RepeatBehavior = RepeatBehavior.Forever
            });
        }

        public void StopLoadingScreen()
        {
            loadingCircleRotate.BeginAnimation(RotateTransform.AngleProperty, null);
            loadingScreen.Visibility = Visibility.Collapsed;
        }

        Path CreateJigsawPiece(JigsawPiece piece)
        {
            double size = piece.Size;
            double fixSize = piece.SideSize;
            
            GeometryGroup group1 = new GeometryGroup { FillRule = FillRule.Nonzero };
            GeometryGroup group2 = new GeometryGroup();
            group2.Children.Add(new RectangleGeometry { Rect = new Rect(0, 0, size, size) });

            bool left = piece.LeftSide == SideType.Active;
            bool top = piece.TopSide == SideType.Active;
            bool right = piece.RightSide == SideType.Active;
            bool bottom = piece.BottomSide == SideType.Active;

            EllipseGeometry e1 = new EllipseGeometry { RadiusX = fixSize, RadiusY = fixSize, Center = new Point(left ? -fixSize : fixSize, size / 2) };
            EllipseGeometry e2 = new EllipseGeometry { RadiusX = fixSize, RadiusY = fixSize, Center = new Point(size / 2, top ? -fixSize : fixSize) };
            EllipseGeometry e3 = new EllipseGeometry { RadiusX = fixSize, RadiusY = fixSize, Center = new Point(right ? size + fixSize : size - fixSize, size / 2) };
            EllipseGeometry e4 = new EllipseGeometry { RadiusX = fixSize, RadiusY = fixSize, Center = new Point(size / 2, bottom ? size + fixSize : size - fixSize) };

            if (piece.LeftSide != SideType.Flat)
            {
                if (left) group1.Children.Add(e1);
                else group2.Children.Add(e1);
            }

            if (piece.TopSide != SideType.Flat)
            {
                if (top) group1.Children.Add(e2);
                else group2.Children.Add(e2);
            }

            if (piece.RightSide != SideType.Flat)
            {
                if (right) group1.Children.Add(e3);
                else group2.Children.Add(e3);
            }

            if (piece.BottomSide != SideType.Flat)
            {
                if (bottom) group1.Children.Add(e4);
                else group2.Children.Add(e4);
            }

            Path path = new Path { Width = size + fixSize * 2 + 1, Height = size + fixSize * 2 + 1, Tag = piece };
            path.Data = new CombinedGeometry { Geometry1 = group1, Geometry2 = group2 };

            return path;
        }

        Menu.JigsawPuzzle currentJigsaw = null;
        DateTime workStartTime = DateTime.MinValue;

        BitmapImage[] tableBackgrounds = new BitmapImage[]
        {
            new BitmapImage(new Uri("pack://application:,,,/Jigsaw;component/Resources/Table1.jpg")),
            new BitmapImage(new Uri("pack://application:,,,/Jigsaw;component/Resources/Table2.jpg")),
            new BitmapImage(new Uri("pack://application:,,,/Jigsaw;component/Resources/Table3.jpg"))
        };

        public void PrepareWorkspace(Menu.JigsawPuzzle jigsawPuzzle)
        {
            currentJigsaw = jigsawPuzzle;
            workStartTime = DateTime.Now;
            bool resetLast = jigsawPuzzle.StartDate != DateTime.MinValue;

            int pieceSize = jigsawPuzzle.PiecePixelSize;
            CreateJigsaw(canvas, jigsawPuzzle, resetLast);

            canvas.Width = jigsawPuzzle.JigsawSize.Width;
            canvas.Height = jigsawPuzzle.JigsawSize.Height;

            Rect inner = new Rect(0, 0, jigsawPuzzle.JigsawSize.Width, jigsawPuzzle.JigsawSize.Height);
            inner.Inflate(pieceSize * 1.3, pieceSize * 1.3);

            double inflateModifer = jigsawPuzzle.PiecesCount > 150 ? (jigsawPuzzle.PiecesCount / (double)140) : 1;
            Rect outer = new Rect(inner.X, inner.Y, inner.Width, inner.Height);
            outer.Inflate(pieceSize * inflateModifer, pieceSize * inflateModifer);

            Rect border = new Rect(0, 0, jigsawPuzzle.JigsawSize.Width, jigsawPuzzle.JigsawSize.Height);
            border.Inflate(35, 35);

            Canvas.SetLeft(jigsawBorder, border.Y);
            Canvas.SetTop(jigsawBorder, border.X);
            jigsawBorder.Width = border.Width;
            jigsawBorder.Height = border.Height;

            if (resetLast == false)
                GroupPiecesAroundJigsawPuzzle(outer, inner, jigsawPuzzle.PiecesCount);

            (background.Fill as ImageBrush).ImageSource = tableBackgrounds[jigsawPuzzle.BackgroundIndex];
            previewImage.ImageSource = jigsawPuzzle.Image;
            preview.Width = (double)jigsawPuzzle.Image.PixelWidth / jigsawPuzzle.Image.PixelHeight * preview.Height;

            background.Width = SystemParameters.PrimaryScreenWidth * 4;
            background.Height = SystemParameters.PrimaryScreenHeight * 4;

            Canvas.SetLeft(background, canvas.Width / 2 - background.Width / 2);
            Canvas.SetTop(background, canvas.Height / 2 - background.Height / 2);

            outer.Inflate(pieceSize * 3, pieceSize * 3);
            double newScale = Width / outer.Width;
            canvasScale.ScaleX = canvasScale.ScaleY = newScale == 0 ? 1 : newScale;

            previewBackground.Width = canvas.Width;
            previewBackground.Height = canvas.Height;

            canvas.Visibility = Visibility.Visible;
            DataInputOutput.SaveJigsawImagesToFileSystem(jigsawPuzzle);

            if (resetLast == false) AddPackingBox(jigsawPuzzle);
            else UpdateStatsLabel();

            StopLoadingScreen();
        }

        void AddPackingBox(Menu.JigsawPuzzle jigsawPuzzle)
        {
            jigsawBorder.Visibility = Visibility.Collapsed;
            for (int i = 0; i < pieces.GetLength(0); i++)
            {
                for (int j = 0; j < pieces.GetLength(1); j++)
                    pieces[i, j].VisualElement.Visibility = Visibility.Collapsed;
            }

            boxImage.Width = jigsawPuzzle.BoxFaceImage.PixelWidth;
            boxImage.Height = jigsawPuzzle.BoxFaceImage.PixelHeight;

            boxImage.Source = jigsawPuzzle.BoxFaceImage;
            (boxImage.Parent as FrameworkElement).Visibility = Visibility.Visible;

            boxImageScale.BeginAnimation(ScaleTransform.ScaleXProperty, new DoubleAnimation
            {
                From = 1.2,
                To = 1,
                Duration = TimeSpan.FromSeconds(0.7),
                EasingFunction = new BounceEase { Bounces = 5 }
            });

            boxBlur.BeginAnimation(DropShadowEffect.ShadowDepthProperty, new DoubleAnimation
            {
                From = 100,
                To = 0,
                Duration = TimeSpan.FromSeconds(0.7),
                EasingFunction = new BounceEase { Bounces = 5 }
            });

            boxOnTablePlayer.Position = TimeSpan.Zero;
            boxOnTablePlayer.Play();
        }

        void boxImage_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            boxOpenPlayer.Position = TimeSpan.Zero;
            boxOpenPlayer.Play();
            
            DoubleAnimation animation = new DoubleAnimation
            {
                To = (Height - (boxImage.Parent as Border).TranslatePoint(new Point(0, 0), this).Y) / canvasScale.ScaleY,
                Duration = TimeSpan.FromSeconds(1),
                EasingFunction = new PowerEase { Power = 5, EasingMode = EasingMode.EaseInOut },
                FillBehavior = FillBehavior.Stop
            };

            animation.Completed += (s, a) =>
            {
                (boxImage.Parent as Border).Visibility = Visibility.Collapsed;
                jigsawBorder.Visibility = Visibility.Visible;

                for (int i = 0; i < pieces.GetLength(0); i++)
                {
                    for (int j = 0; j < pieces.GetLength(1); j++)
                        pieces[i, j].VisualElement.RenderTransform = null;
                }
            };

            Panel.SetZIndex(boxImage.Parent as Border, int.MaxValue);
            boxImageTranslate.BeginAnimation(TranslateTransform.YProperty, animation);

            if (currentJigsaw.StartDate == DateTime.MinValue) currentJigsaw.StartDate = DateTime.Now;
            UpdateStatsLabel();

            for (int i = 0; i < pieces.GetLength(0); i++)
            {
                for (int j = 0; j < pieces.GetLength(1); j++)
                {
                    Path path = pieces[i, j].VisualElement;
                    path.Visibility = Visibility.Visible;

                    TranslateTransform t = new TranslateTransform();
                    path.RenderTransform = t;

                    t.BeginAnimation(TranslateTransform.XProperty, new DoubleAnimation
                    {
                        From = (canvas.Width / 2) - Canvas.GetLeft(path),
                        To = 0,
                        Duration = animation.Duration,
                        EasingFunction = animation.EasingFunction
                    });

                    t.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation
                    {
                        From = (canvas.Height / 2) - Canvas.GetTop(path),
                        To = 0,
                        Duration = animation.Duration,
                        EasingFunction = animation.EasingFunction
                    });
                }
            }
        }

        void UpdateStatsLabel()
        {
            currentJigsaw.PiecesDone = pieces.Cast<JigsawPiece>().Count(c => c.Neighbours.Any());
            int percent = (int)(currentJigsaw.PiecesDone / (double)pieces.Length * 100);
            currentJigsaw.Percentage = percent < 100 ? percent : (pieces[0, 0].Neighbours.Count >= pieces.Length ? 100 : 99);

            jigsawStats.Text = $"Пазл собран на {currentJigsaw.Percentage}%    •    Пазлы {currentJigsaw.PiecesDone}/{pieces.Length}" +
                $"    •    Начало: {currentJigsaw.StartDate:d MMMM HH:mm}";

            if (currentJigsaw.Percentage == 100 && currentJigsaw.Finished == false)
            {
                currentJigsaw.Finished = true;
                new WindowCornerNotification("Пазл полностью собран!");
            }
        }

        Rect lastGroupOuterRect;
        Rect lastGroupInnerRect;

        void GroupPiecesAroundJigsawPuzzle(Rect outerRect, Rect innerRect, int piecesCount, List<JigsawPiece> piecesList = null)
        {
            List<Point> points = GenerateBorderPoints(outerRect, innerRect, piecesCount).OrderBy(o => random.Next()).ToList();
            int iterator = 0;

            lastGroupOuterRect = outerRect;
            lastGroupInnerRect = innerRect;

            if (piecesList == null)
            {
                for (int i = 0; i < pieces.GetLength(0); i++)
                {
                    for (int j = 0; j < pieces.GetLength(1); j++)
                    {
                        Rect bounds = pieces[i, j].VisualElement.Data.Bounds;
                        Canvas.SetLeft(pieces[i, j].VisualElement, points[iterator].X - bounds.Width / 2);
                        Canvas.SetTop(pieces[i, j].VisualElement, points[iterator].Y - bounds.Height / 2);
                        iterator++;
                    }
                }
            }
            else
            {
                foreach (var item in piecesList)
                {
                    Rect bounds = item.VisualElement.Data.Bounds;
                    Canvas.SetLeft(item.VisualElement, points[iterator].X - bounds.Width / 2);
                    Canvas.SetTop(item.VisualElement, points[iterator].Y - bounds.Height / 2);
                    iterator++;
                }
            }
        }

        List<Point> GenerateBorderPoints(Rect outside, Rect inside, int numberOfPoints)
        {
            List<Point> borderPoints = new List<Point>();

            Rect left = new Rect(outside.X, inside.Y, inside.X - outside.X, inside.Height);
            Rect top = new Rect(inside.X, outside.Y, inside.Width, inside.Y - outside.Y);
            Rect right = new Rect(inside.Right, inside.Y, outside.Right - inside.Right, inside.Height);
            Rect bottom = new Rect(inside.X, inside.Bottom, inside.Width, outside.Bottom - inside.Bottom);

            double leftS = left.Width * left.Height;
            double topS = top.Width * top.Height;
            double rightS = right.Width * right.Height;
            double bottomS = bottom.Width * bottom.Height;
            double generalS = leftS + topS + rightS + bottomS;

            GenerateInRect(left, leftS / generalS * numberOfPoints);
            GenerateInRect(top, topS / generalS * numberOfPoints);
            GenerateInRect(right, rightS / generalS * numberOfPoints);
            GenerateInRect(bottom, bottomS / generalS * numberOfPoints);

            void GenerateInRect(Rect rect, double points)
            {
                for (int i = 0; i < points; i++)
                {
                    int x = random.Next((int)rect.X, (int)rect.Right);
                    int y = random.Next((int)rect.Y, (int)rect.Bottom);

                    borderPoints.Add(new Point(x, y));
                }
            }

            return borderPoints;
        }

        void DoEvents()
        {
            Application.Current.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Background,
                                                  new Action(delegate { }));
        }

        void CreateJigsaw(Canvas container, Menu.JigsawPuzzle jigsaw, bool resetLast)
        {
            int w = jigsaw.HorizontalPieces;
            int h = jigsaw.VerticalPieces;

            Rect rect = new Rect(0, 0, jigsaw.PiecePixelSize, jigsaw.PiecePixelSize);
            pieces = new JigsawPiece[w, h];
            double fixSize = jigsaw.PiecePixelSize / 6;

            int loadingIterator = 0;

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    JigsawPiece piece;

                    if (resetLast) piece = currentJigsaw.Pieces[x, y];
                    else
                    {
                        piece = new JigsawPiece { IndexX = x, IndexY = y, Size = jigsaw.PiecePixelSize, SideSize = fixSize };

                        if (x == 0) piece.LeftSide = SideType.Flat;
                        else
                        {
                            if (pieces[x - 1, y].RightSide == SideType.Active) piece.LeftSide = SideType.Passive;
                            else piece.LeftSide = SideType.Active;
                        }

                        if (y == 0) piece.TopSide = SideType.Flat;
                        else
                        {
                            if (pieces[x, y - 1].BottomSide == SideType.Active) piece.TopSide = SideType.Passive;
                            else piece.TopSide = SideType.Active;
                        }

                        if (x == w - 1) piece.RightSide = SideType.Flat;
                        else piece.RightSide = random.NextDouble() > 0.5 ? SideType.Active : SideType.Passive;

                        if (y == h - 1) piece.BottomSide = SideType.Flat;
                        else piece.BottomSide = random.NextDouble() > 0.5 ? SideType.Active : SideType.Passive;
                    }

                    pieces[x, y] = piece;
                    Path path = CreateJigsawPiece(piece);
                    piece.VisualElement = path;

                    path.MouseMove += Path_MouseMove;
                    path.MouseLeftButtonDown += Path_MouseLeftButtonDown;
                    path.MouseLeftButtonUp += Path_MouseLeftButtonUp;

                    if (resetLast)
                    {
                        Canvas.SetLeft(path, currentJigsaw.Pieces[x, y].Location.X);
                        Canvas.SetTop(path, currentJigsaw.Pieces[x, y].Location.Y);
                    }

                    Int32Rect intRect = new Int32Rect((int)(rect.Left + path.Data.Bounds.Left), (int)(rect.Top + path.Data.Bounds.Top),
                        (int)path.Data.Bounds.Width, (int)path.Data.Bounds.Height);

                    CroppedBitmap cropped = new CroppedBitmap(jigsaw.Image, intRect);
                    path.Fill = new ImageBrush(cropped);

                    path.Stroke = new SolidColorBrush { Color = Colors.Black, Opacity = (double)jigsaw.PiecePixelSize / 120 };
                    path.StrokeThickness = 1;

                    container.Children.Add(path);
                    rect.X += jigsaw.PiecePixelSize;

                    loadingProgressText.Text = $"{loadingIterator}/{jigsaw.PiecesCount}";
                    loadingIterator++;
                    DoEvents();
                }

                rect.X = 0;
                rect.Y += jigsaw.PiecePixelSize;
            }
        }

        bool leftButtonDown = false;
        Point mousedownPoint;
        Point dPoint;
        int maxPanelZindex;

        void Path_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            leftButtonDown = true;
            mousedownPoint = e.GetPosition(this);

            Path path = sender as Path;
            dPoint = new Point(Canvas.GetLeft(path), Canvas.GetTop(path));
            path.CaptureMouse();
            path.Cursor = Cursors.None;

            maxPanelZindex++;
            Panel.SetZIndex(path, maxPanelZindex);
            (path.Tag as JigsawPiece).Neighbours.ForEach(f => Panel.SetZIndex(f.VisualElement, maxPanelZindex));
        }

        void Path_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            leftButtonDown = false;
            Path path = sender as Path;
            path.ReleaseMouseCapture();
            CheckJigsawPieceAttaching(path);
            path.Cursor = Cursors.Arrow;

            FrameworkElement parent = canvas.Parent as FrameworkElement;
            Rect parentBounds = parent.RenderTransform.TransformBounds(new Rect(parent.RenderSize));

            if (parentBounds.Contains(e.GetPosition(this)) == false)
                ChangeJissawPiecePosition(path.Tag as JigsawPiece, dPoint.X, dPoint.Y);
        }

        void CheckJigsawPieceAttaching(Path path)
        {
            JigsawPiece piece = path.Tag as JigsawPiece;
            Rect rect = GetPieceBounds(path);
            int shift = 5;

            Point left = new Point(rect.X - shift, rect.Y + rect.Height / 2);
            Point top = new Point(rect.X + rect.Width / 2, rect.Y - shift);
            Point right = new Point(rect.Right + shift, rect.Y + rect.Height / 2);
            Point bottom = new Point(rect.X + rect.Width / 2, rect.Bottom + shift);

            foreach (var item in canvas.Children.OfType<Path>())
            {
                if (item != path)
                {
                    double x = Canvas.GetLeft(item);
                    double y = Canvas.GetTop(item);

                    Rect bounds = new Rect(x + item.Data.Bounds.Left, y + item.Data.Bounds.Top,
                        item.Data.Bounds.Width, item.Data.Bounds.Height);

                    Rect connectBounds = new Rect(x, y, piece.Size, piece.Size);
                    JigsawPiece verifiable = item.Tag as JigsawPiece;

                    if (piece.Neighbours.Contains(verifiable) == false)
                    {
                        if (bounds.Contains(left))
                        {
                            if (IsNeighbour(piece, verifiable, left: true))
                            {
                                ChangeJissawPiecePosition(piece, connectBounds.Right, connectBounds.Top);
                                AddToNeighbours(piece, verifiable);
                            }
                        }
                        else if (bounds.Contains(top))
                        {
                            if (IsNeighbour(piece, verifiable, top: true))
                            {
                                ChangeJissawPiecePosition(piece, connectBounds.Left, connectBounds.Bottom);
                                AddToNeighbours(piece, verifiable);
                            }
                        }
                        else if (bounds.Contains(right))
                        {
                            if (IsNeighbour(piece, verifiable, right: true))
                            {
                                ChangeJissawPiecePosition(piece, connectBounds.Left - connectBounds.Width, connectBounds.Top);
                                AddToNeighbours(piece, verifiable);
                            }
                        }
                        else if (bounds.Contains(bottom))
                        {
                            if (IsNeighbour(piece, verifiable, bottom: true))
                            {
                                ChangeJissawPiecePosition(piece, connectBounds.Left, connectBounds.Top - connectBounds.Height);
                                AddToNeighbours(piece, verifiable);
                            }
                        }
                    }
                }
            }
        }

        void AddToNeighbours(JigsawPiece source, JigsawPiece neighbour)
        {
            source.Neighbours.Add(neighbour);
            AddUniqRange(source.Neighbours, neighbour.Neighbours);

            neighbour.Neighbours.Add(source);
            AddUniqRange(neighbour.Neighbours, source.Neighbours);

            foreach (var item in source.Neighbours)
            {
                if (item.Neighbours.Contains(neighbour) == false)
                    item.Neighbours.Add(neighbour);

                AddUniqRange(item.Neighbours, neighbour.Neighbours);
            }

            foreach (var item in neighbour.Neighbours)
            {
                if (item.Neighbours.Contains(source) == false)
                    item.Neighbours.Add(source);

                AddUniqRange(item.Neighbours, source.Neighbours);
            }

            void AddUniqRange(List<JigsawPiece> list, IEnumerable<JigsawPiece> range)
            {
                foreach (var item in range)
                {
                    if (list.Contains(item) == false)
                        list.Add(item);
                }
            }

            jigsawPiecePlayer.Position = TimeSpan.Zero;
            jigsawPiecePlayer.Play();
            UpdateStatsLabel();
        }

        void ChangeJissawPiecePosition(JigsawPiece piece, double left, double top)
        {
            Canvas.SetLeft(piece.VisualElement, left);
            Canvas.SetTop(piece.VisualElement, top);

            foreach (var item in piece.Neighbours)
            {
                Canvas.SetLeft(item.VisualElement, left - (piece.IndexX - item.IndexX) * piece.Size);
                Canvas.SetTop(item.VisualElement, top - (piece.IndexY - item.IndexY) * piece.Size);
            }
        }

        bool IsNeighbour(JigsawPiece source, JigsawPiece verifiable, bool left = false,
            bool top = false, bool right = false, bool bottom = false)
        {

            if (left)
                return source.IndexY == verifiable.IndexY && source.IndexX == verifiable.IndexX + 1;
            else if (top)
                return source.IndexX == verifiable.IndexX && source.IndexY == verifiable.IndexY + 1;
            else if (right)
                return source.IndexY == verifiable.IndexY && source.IndexX == verifiable.IndexX - 1;
            else if (bottom)
                return source.IndexX == verifiable.IndexX && source.IndexY == verifiable.IndexY - 1;
            else
                return false;
        }

        Rect GetPieceBounds(Path path)
        {
            return new Rect(Canvas.GetLeft(path) + path.Data.Bounds.Left, Canvas.GetTop(path) +
                path.Data.Bounds.Top, path.Data.Bounds.Width, path.Data.Bounds.Height);
        }

        Dictionary<double, double> scalePattern = new Dictionary<double, double>
        {
            { 0, 4 },
            { 0.05, 4.1 },
            { 0.1, 4.2 },
            { 0.15, 4.4 },
            { 0.2, 4.7 },
            { 0.25, 5.4 },
            { 0.3, 6.3 },
            { 0.35, 8 },
            { 0.4, 11.2 },
            { 0.45, 20 },
            { 0.5, 50 },
            { 0.55, 350 },
            { 0.6, 55000 },
            { 1, 100000000 },
        };

        void Path_MouseMove(object sender, MouseEventArgs e)
        {
            if (leftButtonDown)
            {
                Point point = e.GetPosition(this);
                Path path = sender as Path;

                double left = point.X + dPoint.X - mousedownPoint.X;
                double top = point.Y + dPoint.Y - mousedownPoint.Y;

                left /= canvasScale.ScaleX;
                top /= canvasScale.ScaleY;

                double scale = Math.Abs(canvasScale.ScaleX - 0.5);
                double modifer = scalePattern[scalePattern.Select(s => s.Key).Aggregate((x, y) => Math.Abs(x - scale) < Math.Abs(y - scale) ? x : y)];

                left -= dPoint.X / (canvasScale.ScaleX * (canvasScale.ScaleX * modifer));
                top -= dPoint.Y / (canvasScale.ScaleY * (canvasScale.ScaleY * modifer));

                ChangeJissawPiecePosition(path.Tag as JigsawPiece, left, top);
            }
        }

        DependencyObject GetScrollViewer(DependencyObject o)
        {
            if (o is ScrollViewer)
                return o;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(o); i++)
            {
                var child = VisualTreeHelper.GetChild(o, i);
                var result = GetScrollViewer(child);

                if (result == null) continue;
                else return result;
            }

            return null;
        }

        BitmapImage showImage = new BitmapImage(new Uri("pack://application:,,,/Jigsaw;component/Resources/Eye.png"));
        BitmapImage hideImage = new BitmapImage(new Uri("pack://application:,,,/Jigsaw;component/Resources/Hide.png"));

        void Border_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (preview.Visibility == Visibility.Visible)
            {
                ThicknessAnimation animation = new ThicknessAnimation
                {
                    To = new Thickness(50, -200, 0, 0),
                    Duration = TimeSpan.FromSeconds(0.8),
                    EasingFunction = new PowerEase { Power = 5, EasingMode = EasingMode.EaseIn }
                };

                ThicknessAnimation animation2 = new ThicknessAnimation
                {
                    To = new Thickness(50, 20, 0, 0),
                    Duration = animation.Duration,
                    EasingFunction = animation.EasingFunction
                };

                ThicknessAnimation animation3 = new ThicknessAnimation
                {
                    To = new Thickness(160, 20, 0, 0),
                    Duration = animation.Duration,
                    EasingFunction = animation.EasingFunction
                };

                animation.Completed += (s, a) =>
                {
                    preview.Visibility = Visibility.Collapsed;
                    previewPin.Visibility = Visibility.Collapsed;

                    ((showPreviewButton.Child as StackPanel).Children[1] as TextBlock).Text = "Показать фото";
                    ((showPreviewButton.Child as StackPanel).Children[0] as Image).Source = showImage;

                    if (previewPinned)
                        previewPin_MouseLeftButtonUp(null, null);
                };

                showPreviewButton.BeginAnimation(MarginProperty, animation2);
                preview.BeginAnimation(MarginProperty, animation);
                previewPin.BeginAnimation(MarginProperty, animation3);
            }
            else
            {
                preview.Visibility = Visibility.Visible;
                previewPin.Visibility = Visibility.Visible;

                ((showPreviewButton.Child as StackPanel).Children[1] as TextBlock).Text = "Убрать";
                ((showPreviewButton.Child as StackPanel).Children[0] as Image).Source = hideImage;

                ThicknessAnimation animation = new ThicknessAnimation
                {
                    To = new Thickness(50, 0, 0, 0),
                    Duration = TimeSpan.FromSeconds(0.8),
                    EasingFunction = new PowerEase { Power = 5 }
                };

                ThicknessAnimation animation2 = new ThicknessAnimation
                {
                    To = new Thickness(50, 210, 0, 0),
                    Duration = animation.Duration,
                    EasingFunction = animation.EasingFunction
                };

                ThicknessAnimation animation3 = new ThicknessAnimation
                {
                    To = new Thickness(160, 210, 0, 0),
                    Duration = animation.Duration,
                    EasingFunction = animation.EasingFunction
                };

                showPreviewButton.BeginAnimation(MarginProperty, animation2);
                preview.BeginAnimation(MarginProperty, animation);
                previewPin.BeginAnimation(MarginProperty, animation3);
            }
        }

        void overPrevieworder_MouseEnter(object sender, MouseEventArgs e) => previewBackground.Source = currentJigsaw.Image;
        void overPrevieworder_MouseLeave(object sender, MouseEventArgs e)
        {
            if (previewPinned == false)
                previewBackground.Source = null;
        }

        void Border_MouseLeftButtonUp_1(object sender, MouseButtonEventArgs e)
        {
            if (background.Visibility == Visibility.Visible)
            {
                background.Visibility = Visibility.Collapsed;
                (((sender as Border).Child as StackPanel).Children[1] as TextBlock).Text = "Включить фон";
            }
            else
            {
                background.Visibility = Visibility.Visible;
                (((sender as Border).Child as StackPanel).Children[1] as TextBlock).Text = "Выключить фон";
            }
        }

        void Border_MouseLeftButtonUp_2(object sender, MouseButtonEventArgs e)
        {
            List<JigsawPiece> jigsawPieces = pieces.Cast<JigsawPiece>().Where(w =>
                w.Neighbours.Count == 0).ToList();

            if (jigsawPieces.Count == 0)
                ChangeJissawPiecePosition(pieces[0, 0], 0, 0);
            else
            {
                GroupPiecesAroundJigsawPuzzle(lastGroupOuterRect, lastGroupInnerRect,
                    jigsawPieces.Count, jigsawPieces);
            }
        }

        void Border_MouseLeftButtonUp_3(object sender, MouseButtonEventArgs e)
        {
            if (pieces != null && pieces.Length > 0)
            {
                currentJigsaw.Pieces = pieces;

                for (int x = 0; x < pieces.GetLength(0); x++)
                {
                    for (int y = 0; y < pieces.GetLength(1); y++)
                    {
                        canvas.Children.Remove(pieces[x, y].VisualElement);
                        pieces[x, y].Location = new Point(Canvas.GetLeft(pieces[x, y].VisualElement),
                            Canvas.GetTop(pieces[x, y].VisualElement));
                    }
                }

                DataInputOutput.SaveJigsawDataToFileSystem(currentJigsaw);
                currentJigsaw.WorkingTime += DateTime.Now - workStartTime;
            }

            canvas.Visibility = Visibility.Collapsed;
            jigsawStats.Text = string.Empty;

            if (preview.Visibility == Visibility.Visible)
                Border_MouseLeftButtonUp(null, null);

            Menu menu = new Menu(this);
            Panel.SetZIndex(menu, 2);
            grid.Children.Add(menu);

            ScaleTransition(menu, 1.4);
        }

        public void ScaleTransition(UIElement page, double startValue)
        {
            page.RenderTransform = new ScaleTransform();
            page.RenderTransformOrigin = new Point(0.5, 0.5);

            page.RenderTransform.BeginAnimation(ScaleTransform.ScaleXProperty, new DoubleAnimation
            {
                From = startValue,
                To = 1,
                Duration = TimeSpan.FromSeconds(0.5),
                EasingFunction = new PowerEase { Power = 7 }
            });

            page.RenderTransform.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation
            {
                From = startValue,
                To = 1,
                Duration = TimeSpan.FromSeconds(0.5),
                EasingFunction = new PowerEase { Power = 7 }
            });
        }

        void Window_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            Menu menu = grid.Children.OfType<Menu>().FirstOrDefault();
            JigsawCollectionView view = null;
            JigsawFinishedCollectionView viewFinish = null;

            if (menu != null)
            {
                view = (menu.Content as Grid).Children.
                    OfType<JigsawCollectionView>().FirstOrDefault();

                viewFinish = (menu.Content as Grid).Children.
                    OfType<JigsawFinishedCollectionView>().FirstOrDefault();
            }

            if (view != null)
            {
                ScrollViewer scroll = GetScrollViewer(view) as ScrollViewer;
                scroll.ScrollToVerticalOffset(scroll.VerticalOffset + (e.Delta > 0 ? -50 : 50));
            }
            else if (viewFinish != null)
            {
                ScrollViewer scroll = GetScrollViewer(viewFinish) as ScrollViewer;
                scroll.ScrollToVerticalOffset(scroll.VerticalOffset + (e.Delta > 0 ? -50 : 50));
            } 
            else if (boxImageTranslate.X == 0 && canvas.Visibility == Visibility.Visible &&
                ((e.Delta > 0 && canvasScale.ScaleX < 1.5) || (e.Delta < 0 && canvasScale.ScaleX > 0.3))) {

                canvasScale.ScaleX += e.Delta > 0 ? 0.03 : -0.03;
                canvasScale.ScaleY = canvasScale.ScaleX;
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            Border_MouseLeftButtonUp_3(null, null);
            base.OnClosing(e);
        }

        bool previewPinned = false;
        BitmapImage pinImage = new BitmapImage(new Uri("pack://application:,,,/Jigsaw;component/Resources/Pin.png"));
        BitmapImage unpinImage = new BitmapImage(new Uri("pack://application:,,,/Jigsaw;component/Resources/Unpin.png"));

        void previewPin_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (previewPinned == false)
            {
                previewPinned = true;
                previewBackground.Source = currentJigsaw.Image;
                ((previewPin.Child as StackPanel).Children[0] as Image).Source = unpinImage;
            }
            else
            {
                previewPinned = false;
                previewBackground.Source = null;
                ((previewPin.Child as StackPanel).Children[0] as Image).Source = pinImage;
            }
        }
    }
}
