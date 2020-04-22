using System;
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
            InitializeComponent();
            this.DataContext = this;
            BindingOperations.EnableCollectionSynchronization(PackList, _sync);

            using (WebClient wc = new WebClient())
            {
                wc.DownloadFile("https://vps.samoth.eu/packs.json", "packs.json");
            }
            JObject o = JObject.Parse(File.ReadAllText("packs.json"));

            if ((string)o["version"] == "1.0")
            {
                foreach (JObject s in o["instances"])
                {
                    PackList.Add(new MinecraftInstance((string)s["name"], (string)s["description"], (string)s["image"], (string)s["MCVersion"], (string)s["ForgeVersion"], (string)s["instanceName"], (string)s["instanceFolder"], (JArray)s["installProcedure"]));
                }
            }
            else
            {
                //version du fichier non reconnu, mise à jour du logiciel potentiellement disponible
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

        }
    }
}

