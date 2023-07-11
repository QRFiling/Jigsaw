using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;

namespace Jigsaw
{
    class PackageGraphics
    {
        public static double DPI { get; set; } = 1;
        static BitmapImage JigsawPieceIcon = new BitmapImage(new Uri("pack://application:,,,/Jigsaw;component/Resources/JigsawPiece.png"));
        static BitmapImage PlasticTexture = new BitmapImage(new Uri("pack://application:,,,/Jigsaw;component/Resources/PlasticTexture.png"));

        public static BitmapSource CreateJigsawPackingBoxFace(Menu.JigsawPuzzle jigsaw, double width, double height)
        {
            FontFamily font = Application.Current.Resources["MontserratRegular"] as FontFamily;
            Typeface typeface = new Typeface(font, new FontStyle(), new FontWeight(), new FontStretch());

            DrawingVisual visual = new DrawingVisual();
            DrawingContext context = visual.RenderOpen();

            context.DrawRoundedRectangle(Brushes.Silver, null, new Rect(0, 0, width, height), 7, 7);
            context.DrawRoundedRectangle(Brushes.White, null, new Rect(1, 1, width - 2, height - 2), 7, 7);

            Rect contentRect = new Rect(0, 0, width, height);
            contentRect.Inflate(-20, -20);

            context.PushClip(new RectangleGeometry { Rect = contentRect, RadiusX = 10, RadiusY = 10 });
            context.DrawImage(jigsaw.Image, contentRect);
            context.Pop();

            double scaleX = width / 1280;
            double scaleY = scaleX;

            Rect lineRect = new Rect(contentRect.Right - (330 * scaleX), contentRect.Y, 200 * scaleX, contentRect.Height);
            context.DrawRectangle(Brushes.White, null, lineRect);

            string name = jigsaw.Name;
            if (name.Length > 25) name = name.Remove(25) + "...";

            double textDx = 40 * scaleX;
            double textDy = 10 * scaleY;

            context.PushTransform(new ScaleTransform(scaleX, scaleY, lineRect.X + textDx, lineRect.Bottom - textDy));
            context.PushTransform(new RotateTransform(-90, lineRect.X + textDx, lineRect.Bottom - textDy));
            context.DrawText(new FormattedText(name, CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight, typeface, 45, Brushes.Black, DPI), new Point(lineRect.X + textDx, lineRect.Bottom - textDy));

            context.PushTransform(new TranslateTransform(2, 60));
            context.DrawText(new FormattedText(jigsaw.Creator + "  •  " + jigsaw.CentimetreSizeString, CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight, typeface, 25, Brushes.DimGray, DPI), new Point(lineRect.X + textDx, lineRect.Bottom - textDy));

            context.Pop();
            context.Pop();
            context.Pop();
            context.PushTransform(new ScaleTransform(scaleX, scaleY));

            FormattedText pieceText = new FormattedText(jigsaw.PiecesCount.ToString(), CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight, typeface, 40, Brushes.White, DPI);

            Rect piecesBar = new Rect(contentRect.X + 50, contentRect.Y + 50, pieceText.Width + 130, 80);
            context.DrawRoundedRectangle(new SolidColorBrush(Color.FromRgb(16, 113, 75)), null, piecesBar, 10, 10);

            context.DrawText(pieceText, new Point(piecesBar.X + piecesBar.Width / 2 - pieceText.Width / 2 - 17,
                piecesBar.Y + piecesBar.Height / 2 - pieceText.Height / 2));

            context.DrawImage(JigsawPieceIcon, new Rect(piecesBar.Right - 77, piecesBar.Top + 21, 38, 38));
            context.Pop();

            context.PushOpacity(0.2);
            Rect texture = new Rect(0, 0, width, height);
            context.PushClip(new RectangleGeometry { Rect = texture, RadiusX = 15, RadiusY = 15 });
            context.DrawImage(PlasticTexture, texture);

            context.Close();
            RenderTargetBitmap bmp = new RenderTargetBitmap((int)width, (int)height, 96, 96, PixelFormats.Pbgra32);
            bmp.Render(visual);

            return bmp;
        }

        public static BitmapSource CreateJigsawPackageBox(BitmapSource face, double width, double height)
        {
            DrawingVisual visual = new DrawingVisual();
            DrawingContext context = visual.RenderOpen();

            const double modifer = 0.95;
            const double negModifer = 1 - modifer;

            Rect rect = new Rect(0, 0, width * modifer, height * modifer);
            context.DrawImage(face, rect);

            PathGeometry path = new PathGeometry();
            PathFigure figure = new PathFigure();
            path.Figures.Add(figure);

            PolyLineSegment poly = new PolyLineSegment();
            poly.Points.Add(new Point(width * negModifer, height * negModifer));
            poly.Points.Add(new Point(width * negModifer, height * (1 + negModifer)));
            poly.Points.Add(new Point(0, height * modifer));
            figure.Segments.Add(poly);

            context.PushTransform(new TranslateTransform(width * modifer, 0));
            context.DrawGeometry(new SolidColorBrush(Colors.Gainsboro), null, path);
            context.Pop();

            PathGeometry path2 = new PathGeometry();
            PathFigure figure2 = new PathFigure();
            path2.Figures.Add(figure2);

            PolyLineSegment poly2 = new PolyLineSegment();
            poly2.Points.Add(new Point(width * modifer, 0));
            poly2.Points.Add(new Point(width, height * negModifer));
            poly2.Points.Add(new Point(width * negModifer, height * negModifer));
            figure2.Segments.Add(poly2);

            context.PushTransform(new TranslateTransform(0, height * modifer));
            context.DrawGeometry(new SolidColorBrush(Colors.Silver), null, path2);
            context.Pop();

            context.Close();
            RenderTargetBitmap bmp = new RenderTargetBitmap((int)width, (int)height, 96, 96, PixelFormats.Pbgra32);

            bmp.Render(visual);
            return bmp;
        }
    }
}
