using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Jigsaw
{
    class DataInputOutput
    {
        public static void SaveJigsawImagesToFileSystem(Menu.JigsawPuzzle jigsaw)
        {
            try
            {
                string path = $@"Saved\Jigsaw{jigsaw.CreationDate.ToFileTime()}";
                Directory.CreateDirectory(path);

                SaveImage(jigsaw.Image, "pic1");
                SaveImage(jigsaw.PreviewBoxLikeImage, "pic2");
                SaveImage(jigsaw.BoxFaceImage, "pic3");

                void SaveImage(BitmapSource image, string name)
                {
                    using (var fileStream = new FileStream($@"{path}\{name}", FileMode.Create))
                    {
                        BitmapEncoder encoder = new PngBitmapEncoder();
                        encoder.Frames.Add(BitmapFrame.Create(image));
                        encoder.Save(fileStream);
                    }
                }
            }
            catch { }
        }

        public static void SaveJigsawDataToFileSystem(Menu.JigsawPuzzle jigsaw)
        {
            try
            {
                string path = $@"Saved\Jigsaw{jigsaw.CreationDate.ToFileTime()}";
                Directory.CreateDirectory(path);

                using (FileStream stream = new FileStream($@"{path}\data", FileMode.Create))
                {
                    BinaryFormatter formatter = new BinaryFormatter();
                    formatter.Serialize(stream, jigsaw);
                }
            }
            catch { }
        }

        public static List<Menu.JigsawPuzzle> LoadJigsawsFromFileSystem()
        {
            List<Menu.JigsawPuzzle> jigsawPuzzles = new List<Menu.JigsawPuzzle>();
            string folder = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\Saved";

            if (Directory.Exists(folder))
            {
                BinaryFormatter formatter = new BinaryFormatter();

                foreach (var item in Directory.GetDirectories(folder))
                {
                    try
                    {  
                        using (var fileStream = new FileStream($@"{item}\data", FileMode.Open))
                        {
                            Menu.JigsawPuzzle jigsaw = formatter.Deserialize(fileStream) as Menu.JigsawPuzzle;

                            jigsaw.Image = OpenImage($@"{item}\pic1");
                            jigsaw.PreviewBoxLikeImage = OpenImage($@"{item}\pic2");
                            jigsaw.BoxFaceImage = OpenImage($@"{item}\pic3");

                            jigsawPuzzles.Add(jigsaw);
                        }
                    }
                    catch { }
                }
            }

            return jigsawPuzzles;
        }

        public static BitmapSource OpenImage(string path)
        {
            var bitmap = new BitmapImage();
            var stream = File.OpenRead(path);

            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.StreamSource = stream;
            bitmap.EndInit();
            stream.Close();
            stream.Dispose();

            return bitmap;
        }

        public static bool RemoveJigsawFromFileSystem(Menu.JigsawPuzzle jigsaw)
        {
            string path = $@"Saved\Jigsaw{jigsaw.CreationDate.ToFileTime()}";

            if (Directory.Exists(path))
            {
                try { Directory.Delete(path, true); }
                catch { return false; }
            }
            else return false;
            return true;
        }

        public static void InitializePlayer(MediaPlayer player, string soundName)
        {
            try
            {
                string path = $@"Audio\{soundName}.mp3";
                if (File.Exists(path)) player.Open(new Uri(path, UriKind.Relative));
            }
            catch { }
        }

        public static string GetFileSizeString(string path)
        {
            long byteCount = new FileInfo(path).Length;           
            string[] suf = { "B", "KB", "MB", "GB", "TB", "PB", "EB" };
            if (byteCount == 0) return "0" + suf[0];
            long bytes = Math.Abs(byteCount);
            int place = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));
            double num = Math.Round(bytes / Math.Pow(1024, place), 1);
            return (Math.Sign(byteCount) * num).ToString() + suf[place];
        }
    }
}
