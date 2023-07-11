using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Jigsaw
{
    /// <summary>
    /// Логика взаимодействия для App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static SplashScreen splashScreen;

        protected override void OnStartup(StartupEventArgs e)
        {
            splashScreen = new SplashScreen("SplashScreen3.png");
            splashScreen.Show(false);

            base.OnStartup(e);
        }
    }
}
