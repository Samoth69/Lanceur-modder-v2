using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Lanceur_Modder_v2
{
    #region Installation Module

    public abstract class InstallationModule
    {
        public abstract string description { get; }
        public delegate void ProgressEventHandler(object o, ProgressEventArgs pa);
        public event ProgressEventHandler OnProgressUpdateEventHandler;
        public abstract void DoWork(string instancePath);

        protected void OnProgressUpdate(object sender, ProgressEventArgs pea)
        {
            OnProgressUpdateEventHandler(sender, pea);
        }
    }

    public class ProgressEventArgs : EventArgs
    {
        private string title;
        private double progress;
        public ProgressEventArgs()
        {
            title = "null";
            progress = 0.0;
        }
        public ProgressEventArgs(string title, double progress)
        {
            this.title = title;
            this.progress = progress;
        }

        public string Title { get => title; set => title = value; }
        public double Progress { get => progress; set => progress = value; }
    }

    public class DownloadModule : InstallationModule
    {
        private string _description;
        private List<DownloadPath> _installProcedure = new List<DownloadPath>();
        private ProgressEventArgs pea = new ProgressEventArgs();
        //private BackgroundWorker bgw = new BackgroundWorker();
        public DownloadModule(string description, JArray installProcedure)
        {
            if (installProcedure != null)
            {
                foreach (JObject o in installProcedure)
                {
                    _installProcedure.Add(new DownloadPath((string)o["downloadLink"], (string)o["destinationPath"]));
                }
            }
            _description = description;
        }

        public override string description => _description;

        public override void DoWork(string instancePath)
        {
            //bgw.DoWork += bgw_doWork;
            //bgw.ProgressChanged += bgw_ProgressChanged;
            //bgw.WorkerReportsProgress = true;
            using (WebClient wc = new WebClient())
            {
                wc.DownloadProgressChanged += DownloadStatusUpdate;
                foreach (DownloadPath dp in _installProcedure)
                {
                    pea.Title = Path.GetFileNameWithoutExtension(dp.DestinationPath);
                    wc.DownloadFile(dp.DownloadLink, dp.DestinationPath.Replace("$IntancePath", instancePath));
                }
            }
        }


        private void DownloadStatusUpdate(object sender, DownloadProgressChangedEventArgs e)
        {
            pea.Progress = e.ProgressPercentage;
            OnProgressUpdate(this, pea);
        }
    }

    public class DownloadPath
    {
        private string downloadLink;
        private string destinationPath;

        public DownloadPath(string downloadLink, string destinationPath)
        {
            this.downloadLink = downloadLink;
            this.destinationPath = destinationPath;
        }

        public string DownloadLink { get => downloadLink; }
        public string DestinationPath { get => destinationPath; }
    }

    #endregion
}
