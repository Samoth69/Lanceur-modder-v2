using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net;
using System.IO;
using System.ComponentModel;
using CmlLib;
using CmlLib.Core;

namespace Lanceur_Modder_v2
{
    #region Installation Module

    public abstract class InstallationModule
    {
        protected abstract MinecraftInstance parent { get; }
        protected abstract string description { get; }
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
        private string description;
        private string moduleDescription;
        private int progress;
        private int max;
        public ProgressEventArgs()
        {
            title = "null";
            progress = 0;
            Max = 0;
        }
        public ProgressEventArgs(string title, int progress)
        {
            this.title = title;
            this.progress = progress;
            this.max = 100;
        }

        public ProgressEventArgs(string title, int progress, int max)
        {
            this.title = title;
            this.progress = progress;
            this.max = max;
        }

        public string Title { get => title; set => title = value; }
        public int Progress { get => progress; set => progress = value; }
        public int Max { get => max; set => max = value; }
        public string Description { get => description; set => description = value; }
        public string ModuleDescription { get => moduleDescription; set => moduleDescription = value; }
    }

    public class DownloadModule : InstallationModule
    {
        private MinecraftInstance _parent;
        protected override MinecraftInstance parent => _parent;
        private string _description;
        private List<DownloadPath> _installProcedure = new List<DownloadPath>();
        private ProgressEventArgs pea = new ProgressEventArgs();
        private double oldMByteReceived = 0;
        public DownloadModule(MinecraftInstance parent, string description, JArray installProcedure)
        {
            _parent = parent;
            if (installProcedure != null && _parent != null)
            {
                string path;
                foreach (JObject o in installProcedure)
                {
                    path = (string)o["destinationPath"];
                    path = path.Replace("$InstancePath", _parent.InstanceFolder);
                    _installProcedure.Add(new DownloadPath((string)o["downloadLink"], path));
                }
            }
            _description = description;
            pea.ModuleDescription = _description;
        }

        protected override string description => _description;

        public override void DoWork(string instancePath)
        {
            pea.Max = _installProcedure.Count;
            using (WebClient wc = new WebClient())
            {
                wc.DownloadProgressChanged += DownloadStatusUpdate;
                wc.DownloadFileCompleted += DownloadComplete;
                foreach (DownloadPath dp in _installProcedure)
                {
                    pea.Title = Path.GetFileNameWithoutExtension(dp.DestinationPath);
                    if (!Directory.Exists(Path.GetDirectoryName(dp.DestinationPath)))
                        Directory.CreateDirectory(Path.GetDirectoryName(dp.DestinationPath));
                    wc.DownloadFileAsync(new Uri(dp.DownloadLink), dp.DestinationPath);
                    while (wc.IsBusy) { };
                    pea.Progress++;
                    //System.Threading.Thread.Sleep(500);
                }
            }
        }

        private void DownloadComplete(object sender, AsyncCompletedEventArgs e)
        {
            pea.Progress = pea.Progress;
            OnProgressUpdate(this, pea);
        }

        private void DownloadStatusUpdate(object sender, DownloadProgressChangedEventArgs e)
        {
            double MbyteReceived = Math.Round((double)e.BytesReceived / 1000000, 2);
            double MbyteTotal = Math.Round((double)e.TotalBytesToReceive / 1000000, 2);
            if (oldMByteReceived != 0)
                pea.Description = MbyteReceived.ToString() + "/" + MbyteTotal.ToString() + "MB @" + Math.Round(MbyteReceived - oldMByteReceived, 2) + "MB/s";
            else
                pea.Description = MbyteReceived.ToString() + "/" + MbyteTotal.ToString() + "MB @N/A MB/s";
            oldMByteReceived = MbyteReceived;
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

    public class MinecraftInstallationModule : InstallationModule
    {
        private MinecraftInstance _parent;
        protected override MinecraftInstance parent => _parent;

        private string _description;
        protected override string description => _description;

        private string _mcversion;

        private ProgressEventArgs pea = new ProgressEventArgs();

        public MinecraftInstallationModule(MinecraftInstance parent, string mcversion, string description)
        {
            _parent = parent;
            _mcversion = mcversion;
            _description = description;
            pea.ModuleDescription = _description;
            pea.Max = 100;
        }

        public override void DoWork(string instancePath)
        {
            CMLauncher Launcher = new CMLauncher(instancePath);
            Launcher.FileChanged += Downloader_ChangeFile;
            Launcher.ProgressChanged += Downloader_ChangeProgress;
            Launcher.UpdateProfiles();
        }

        private void Downloader_ChangeProgress(object sender, System.ComponentModel.ProgressChangedEventArgs e)
        {
            pea.Progress = e.ProgressPercentage;
            OnProgressUpdate(sender, pea);
        }

        private void Downloader_ChangeFile(DownloadFileChangedEventArgs e)
        {
            //Console.WriteLine("Now Downloading : {0} - {1} ({2}/{3})", e.FileKind, e.FileName, e.ProgressedFileCount, e.TotalFileCount);
            pea.Title = e.FileKind.ToString();
            pea.Description = "[" + e.ProgressedFileCount + "/" + e.TotalFileCount + "] " + e.FileName;
            OnProgressUpdate(this, pea); 
        }
    }

    #endregion
}
