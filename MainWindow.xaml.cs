using CmlLib.Core;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Net;
using System.IO;
using Newtonsoft.Json.Linq;

namespace Lanceur_Modder_v2
{
    /// <summary>
    /// Logique d'interaction pour MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        List<MinecraftInstance> PackList = new List<MinecraftInstance>();

        public MainWindow()
        {
            //PackList.Add(new MinecraftInstance("Test", "Description\npozeriçuazeyriouyroi", "image.png"));
            //PackList.Add(new MinecraftInstance("BugDroid", "Voici notre amis 'BugDroid', promis il mord pas", "android.png"));
            //PackList[0].Image.UriSource = new Uri(@"image.png", UriKind.RelativeOrAbsolute);

            using (WebClient wc = new WebClient())
            {
                wc.DownloadFile("https://vps.samoth.eu/packs.json", "packs.json");
            }
            JObject o = JObject.Parse(File.ReadAllText("packs.json"));

            foreach (JObject s in o["instances"])
            {
                PackList.Add(new MinecraftInstance((string)s["name"], (string)s["description"], (string)s["image"], (string)s["MCVersion"], (string)s["instanceName"], (string)s["instanceFolder"], (JArray)s["installProcedure"]));
            }

            InitializeComponent();

            LVPacksList.ItemsSource = PackList;
            MinecraftInstance.OnUpdateScreen += UpdateScreen;
        }

        private void UpdateScreen(object sender)
        {
            
        }

        private void ToolBar_Loaded(object sender, RoutedEventArgs e)
        {
            ToolBar toolBar = sender as ToolBar;
            var overflowGrid = toolBar.Template.FindName("OverflowGrid", toolBar) as FrameworkElement;
            if (overflowGrid != null)
            {
                overflowGrid.Visibility = Visibility.Collapsed;
            }
            var mainPanelBorder = toolBar.Template.FindName("MainPanelBorder", toolBar) as FrameworkElement;
            if (mainPanelBorder != null)
            {
                mainPanelBorder.Margin = new Thickness();
            }
        }

        private void Refresh_Button_Click(object sender, RoutedEventArgs rea)
        {
            var login = new MLogin();
            //var session = login.TryAutoLogin();
            //if (session.Result != MLoginResult.Success) // failed to auto login
            //{
            var email = "****";
            var pw = "****";
            var session = login.Authenticate(email, pw);

            if (session.Result != MLoginResult.Success)
                throw new Exception(session.Result.ToString()); // failed to login
            //}

            MessageBox.Show("login info:\n" + session.Username + "\n" + session.Message);

            var path = new Minecraft(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\Minecraft\\");
            //var path = Minecraft.GetOSDefaultPath(); // mc directory

            var launcher = new CmlLib.CMLauncher(path);
            launcher.ProgressChanged += (s, e) =>
            {
                Console.Write("{0}% ", e.ProgressPercentage);
            };
            launcher.FileChanged += (e) =>
            {
                Console.WriteLine("[{0}] {1} - {2}/{3}", e.FileKind.ToString(), e.FileName, e.ProgressedFileCount, e.TotalFileCount);
            };

            launcher.UpdateProfiles();
            foreach (var item in launcher.Profiles)
            {
                Console.WriteLine(item.Name);
            }

            var launchOption = new MLaunchOption
            {
                MaximumRamMb = 1024,
                Session = session, // Login Session. ex) Session = MSession.GetOfflineSession("hello")

                //LauncherName = "MyLauncher",
                //ScreenWidth = 1600,
                //ScreenHeigth = 900,
                //ServerIp = "mc.hypixel.net"
            };

            // launch forge
            var process = launcher.CreateProcess("1.12.2", launchOption);

            // launch vanila
            //var process = launcher.Launch("1.15.2", launchOption);

            process.Start();
        }
    }
}

