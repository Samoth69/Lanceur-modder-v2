using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.IO;
using System.Security;
using CmlLib.Core;
using System.Threading;
using System.Diagnostics;
using CmlLib;
using System.Runtime.InteropServices;

namespace Lanceur_Modder_v2
{
    public class MinecraftInstance : IDisposable, INotifyPropertyChanged
    {
        private string _name;
        private BitmapImage _Image;
        private string _Description;
        private string _MCVersion;
        private string _ForgeVersion;
        private string _instanceName;
        private string _instanceFolder;
        private List<InstallationModule> _installProcedure = new List<InstallationModule>();
        private BackgroundWorker bgw = new BackgroundWorker();
        // Track whether Dispose has been called.
        private bool disposed = false;

        private const string InstanceFolderName = "Instances";

        private string _mainProgressText;
        private int _progressBarValue;
        private int _progressBarMaxValue;
        private string _detailProgressText;
        private bool _currentlyDownloading = false;
        private string _buttonInstallText = "Installer";

        private ICommand _installCommand;

        //event pour update l'IHM
        public event PropertyChangedEventHandler PropertyChanged;

        //event pour mettre à jour le status de téléchargement
        public delegate void ProgressEventHandler(object o, ProgressEventArgs pa);
        public event ProgressEventHandler OnProgressUpdateEventHandler;

        protected void OnProgressUpdate(object sender, ProgressEventArgs pea)
        {
            OnProgressUpdateEventHandler(sender, pea);
        }

        private bool _installer = false;

        public MinecraftInstance(string Name, string Description, string ImagePath, string MCVersion, string ForgeVersion, string instanceName, string instanceFolder, JArray actionObject)
        {
            _name = Name;
            _Description = Description;

            _Image = new BitmapImage();
            _Image.BeginInit();
            _Image.UriSource = new Uri(ImagePath);
            _Image.CacheOption = BitmapCacheOption.OnLoad;
            _Image.EndInit();

            _MCVersion = MCVersion;
            _ForgeVersion = ForgeVersion;
            _instanceName = instanceName;
            _instanceFolder = instanceFolder;
            if (_instanceFolder != null)
                _instanceFolder = InstanceFolderName + "\\" + _instanceFolder;

            if (actionObject != null)
            {
                foreach (JObject o in actionObject)
                {
                    switch ((string)o["action"])
                    {
                        case "download":
                            _installProcedure.Add(new DownloadModule(this, (string)o["description"], (JArray)o["downloadLinks"]));
                            break;
                        case "installMinecraft":
                            _installProcedure.Add(new MinecraftInstallationModule(this, _MCVersion, _Description));
                            break;
                        default:
                            //warning sur les logs à ajouter
                            break;
                    }
                }
            }

            bgw.DoWork += Bgw_DoWork;
            bgw.RunWorkerCompleted += Bgw_Finished;

            foreach (InstallationModule im in _installProcedure)
            {
                im.OnProgressUpdateEventHandler += Bgw_ProgressUpdate;
            }

            if (File.Exists(_instanceFolder + "\\packInfo.json"))
            {
                string t = File.ReadAllText(_instanceFolder + "\\packInfo.json");
                if (t == "0")
                    _installer = false;
                else if (t == "1")
                {
                    _installer = true;
                    ButtonInstallText = "Jouer";
                }
            }
            else
            {
                _installer = false;
            }

            OnProgressUpdateEventHandler += MinecraftInstance_OnProgressUpdateEventHandler;
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(propertyName));
        }

        public string Name { get => _name; }
        public BitmapImage Image { get => _Image; }
        public string Description { get => _Description; }
        public string MCVersion { get => _MCVersion; }
        public string InstanceName { get => _instanceName; }
        public string InstanceFolder { get => _instanceFolder; set => _instanceFolder = value; }

        public ICommand InstallCommand
        {
            get
            {
                if (_installCommand == null)
                {
                    _installCommand = new RelayCommand(
                        param => this.InstallButtonClick(),
                        param => this.CanSave()
                    );
                }
                return _installCommand;
            }
        }

        private bool CanSave() { return true; }

        public string MainProgressText { 
            get => _mainProgressText; 
            set 
            { 
                _mainProgressText = value;
                OnPropertyChanged(nameof(MainProgressText));
            } 
        }
        public int ProgressBarValue
        {
            get => _progressBarValue;
            set
            {
                _progressBarValue = value;
                OnPropertyChanged(nameof(ProgressBarValue));
            }
        }
        public int ProgressBarMaxValue { 
            get => _progressBarMaxValue;
            set
            {
                _progressBarMaxValue = value;
                OnPropertyChanged(nameof(ProgressBarMaxValue));
            }
        }
        public string DetailProgressText { 
            get => _detailProgressText;
            set
            {
                _detailProgressText = value;
                OnPropertyChanged(nameof(DetailProgressText));
            }
        }
        public bool CurrentlyDownloading
        {
            get => _currentlyDownloading;
            set
            {
                
                _currentlyDownloading = value;
                OnPropertyChanged(nameof(CurrentlyDownloading));
                OnPropertyChanged(nameof(CurrentlyDownloadingReversed));
            }
        }

        public bool CurrentlyDownloadingReversed { get => !_currentlyDownloading; }
        
        public string ButtonInstallText { 
            get => _buttonInstallText;
            set 
            { 
                _buttonInstallText = value;
                OnPropertyChanged(nameof(ButtonInstallText));
            } 
        }

        public bool Installer { get => _installer; }

        private ProgressEventArgs pea = new ProgressEventArgs();

        private void InstallButtonClick()
        {
            bgw.RunWorkerAsync();
        }

        private void Launcher_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            pea.Progress = e.ProgressPercentage;
            OnProgressUpdate(this, pea);
        }

        private void Launcher_FileChanged(DownloadFileChangedEventArgs e)
        {
            pea.Title = "[" + e.ProgressedFileCount + "/" + e.TotalFileCount + "] " + e.FileName;
            OnProgressUpdate(this, pea);
        }

        private void MinecraftInstance_OnProgressUpdateEventHandler(object o, ProgressEventArgs pa)
        {
            DetailProgressText = "(" + pa.Progress + "%) " + pa.Title;
            ProgressBarValue = pa.Progress;
        }

        private String SecureStringToString(SecureString value)
        {
            IntPtr valuePtr = IntPtr.Zero;
            try
            {
                valuePtr = Marshal.SecureStringToGlobalAllocUnicode(value);
                return Marshal.PtrToStringUni(valuePtr);
            }
            finally
            {
                Marshal.ZeroFreeGlobalAllocUnicode(valuePtr);
            }
        }

        #region backgroundworker

        private int numberOfOperation = 0;
        private int currentOperation = 1;

        private void Bgw_DoWork(object sender, DoWorkEventArgs e)
        {
            CurrentlyDownloading = true;
            if (!Installer)
            {
                ButtonInstallText = "Installation...";
                MainProgressText = "Installation";
                numberOfOperation = _installProcedure.Count;
                foreach (InstallationModule im in _installProcedure)
                {
                    im.OnProgressUpdateEventHandler += Bgw_ProgressUpdate;
                    im.DoWork(_instanceFolder);
                    im.OnProgressUpdateEventHandler -= Bgw_ProgressUpdate;
                    currentOperation++;
                }
            }
            else
            {
                MinecraftLoginWindow MLW;
                CMLauncher launcher = new CMLauncher(InstanceFolder);

                launcher.FileChanged += Launcher_FileChanged;
                launcher.ProgressChanged += Launcher_ProgressChanged;
                ProgressBarMaxValue = 100;
                MainProgressText = "Démarrage...";

                MLogin ml = new MLogin();
                MSession session = new MSession();
                bool dialogResult = true;

                session = ml.TryAutoLogin();
                if (string.IsNullOrEmpty(session.AccessToken))
                {
                    do
                    {
                        Application.Current.Dispatcher.Invoke((Action)delegate{
                            MLW = new MinecraftLoginWindow();
                            dialogResult = (bool)MLW.ShowDialog();
                            if (dialogResult)
                            {
                                SecureString ss = MLW.Passwd;

                                session = ml.Authenticate(MLW.Email, SecureStringToString(ss));
                                ss.Dispose();
                            }
                        });
                        
                        if (dialogResult && (session != null && session.Result != MLoginResult.Success))
                        {
                            MessageBox.Show("Erreur d'authentifcation: " + session.Result.ToString() + "\n" + session.Message);
                        }
                    } while (dialogResult && session.Result != MLoginResult.Success);
                }

                if (dialogResult)
                {
                    var launchOption = new MLaunchOption()
                    {
                        MaximumRamMb = 2048,
                        Session = session
                    };

                    var th = new Thread(() =>
                    {
                        Process mc = new Process();

                        mc = launcher.CreateProcess(_MCVersion, _ForgeVersion, launchOption);
                        mc.StartInfo.UseShellExecute = false;

                        mc.Start();
                    });
                    th.Start();

                    while (th.IsAlive) { Thread.Sleep(1); }

                }
            }
            CurrentlyDownloading = false;
        }

        private void Bgw_ProgressUpdate(object sender, ProgressEventArgs e)
        {
            ProgressBarMaxValue = e.Max;
            ProgressBarValue = e.Progress;
            DetailProgressText = "(" + e.Progress + "%) " + e.Description;
            MainProgressText = "Installation " + e.ModuleDescription + " [" + currentOperation + "/" + numberOfOperation + "]: " + e.Title;
        }

        private void Bgw_Finished(object sender, RunWorkerCompletedEventArgs e)
        {
            CurrentlyDownloading = false;
            _installer = true;
            File.WriteAllText(_instanceFolder + "\\packInfo.json", "1");
            ButtonInstallText = "Jouer";
        }


        // Implement IDisposable.
        // Do not make this method virtual.
        // A derived class should not be able to override this method.
        public void Dispose()
        {
            Dispose(true);
            // This object will be cleaned up by the Dispose method.
            // Therefore, you should call GC.SupressFinalize to
            // take this object off the finalization queue
            // and prevent finalization code for this object
            // from executing a second time.
            GC.SuppressFinalize(this);
        }

        // Dispose(bool disposing) executes in two distinct scenarios.
        // If disposing equals true, the method has been called directly
        // or indirectly by a user's code. Managed and unmanaged resources
        // can be disposed.
        // If disposing equals false, the method has been called by the
        // runtime from inside the finalizer and you should not reference
        // other objects. Only unmanaged resources can be disposed.
        protected virtual void Dispose(bool disposing)
        {
            // Check to see if Dispose has already been called.
            if (!this.disposed)
            {
                // If disposing equals true, dispose all managed
                // and unmanaged resources.
                if (disposing)
                {
                    bgw.Dispose();
                }

                // Call the appropriate methods to clean up
                // unmanaged resources here.
                // If disposing is false,
                // only the following code is executed.
                //CloseHandle(handle);
                //handle = IntPtr.Zero;

                // Note disposing has been done.
                disposed = true;
            }
        }
        // Use interop to call the method necessary
        // to clean up the unmanaged resource.
        [System.Runtime.InteropServices.DllImport("Kernel32")]
        private extern static Boolean CloseHandle(IntPtr handle);

        // Use C# destructor syntax for finalization code.
        // This destructor will run only if the Dispose method
        // does not get called.
        // It gives your base class the opportunity to finalize.
        // Do not provide destructors in types derived from this class.
        ~MinecraftInstance()
        {
            // Do not re-create Dispose clean-up code here.
            // Calling Dispose(false) is optimal in terms of
            // readability and maintainability.
            Dispose(false);
        }

        #endregion
    }
}
