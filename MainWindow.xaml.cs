using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Net;
using System.IO;
using Newtonsoft.Json.Linq;
using System.Collections.ObjectModel;
using System.Windows.Data;

namespace Lanceur_Modder_v2
{
    /// <summary>
    /// Logique d'interaction pour MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly object _sync = new object();
        public ObservableCollection<MinecraftInstance> PackList { get; } = new ObservableCollection<MinecraftInstance>();

        public MainWindow()
        {
            //PackList.Add(new MinecraftInstance("Test", "Description\npozeriçuazeyriouyroi", "image.png"));
            //PackList.Add(new MinecraftInstance("BugDroid", "Voici notre amis 'BugDroid', promis il mord pas", "android.png"));
            //PackList[0].Image.UriSource = new Uri(@"image.png", UriKind.RelativeOrAbsolute);
            InitializeComponent();
            this.DataContext = this;
            BindingOperations.EnableCollectionSynchronization(PackList, _sync);

            using (WebClient wc = new WebClient())
            {
                wc.DownloadFile("https://vps.samoth.eu/packs.json", "packs.json");
            }
            JObject o = JObject.Parse(File.ReadAllText("packs.json"));

            foreach (JObject s in o["instances"])
            {
                PackList.Add(new MinecraftInstance((string)s["name"], (string)s["description"], (string)s["image"], (string)s["MCVersion"], (string)s["instanceName"], (string)s["instanceFolder"], (JArray)s["installProcedure"]));
            }
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
            MinecraftLoginWindow MLW = new MinecraftLoginWindow();
            if (MLW.ShowDialog() == true)
            {
                MessageBox.Show("yes");
            }
        }
    }
}

