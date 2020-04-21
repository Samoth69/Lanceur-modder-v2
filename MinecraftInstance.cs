using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace Lanceur_Modder_v2
{
    public class MinecraftInstance : IDisposable, INotifyPropertyChanged
    {
        private string _name;
        private BitmapImage _Image;
        private string _Description;
        private string _MCVersion;
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
        //private Visibility _currentlyDownloading = Visibility.Visible;
        private bool _currentlyDownloading = false;
        private string _buttonInstallText = "Installer";

        private ICommand _installCommand;

        //public delegate void UpdateScreen(object sender);
        //public static event UpdateScreen OnUpdateScreen;
        public event PropertyChangedEventHandler PropertyChanged;

        public MinecraftInstance(string Name, string Description, string ImagePath, string MCVersion, string instanceName, string instanceFolder, JArray actionObject)
        {
            _name = Name;
            _Description = Description;

            _Image = new BitmapImage();
            _Image.BeginInit();
            _Image.UriSource = new Uri(ImagePath);
            _Image.CacheOption = BitmapCacheOption.OnLoad;
            _Image.EndInit();

            _MCVersion = MCVersion;
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

        private bool CanSave()
        {
            // Verify command can be executed here
            return true;
        }

        private void InstallButtonClick()
        {
            // Save command execution logic
            //MessageBox.Show(_name);
            CurrentlyDownloading = true;
            ButtonInstallText = "Installation...";
            MainProgressText = "Installation";
            bgw.RunWorkerAsync();
            //OnUpdateScreen(this);
        }

        #region backgroundworker

        private int numberOfOperation = 0;
        private int currentOperation = 1;

        private void Bgw_DoWork(object sender, DoWorkEventArgs e)
        {
            numberOfOperation = _installProcedure.Count;
            foreach (InstallationModule im in _installProcedure)
            {
                im.OnProgressUpdateEventHandler += Bgw_ProgressUpdate;
                im.DoWork(_instanceFolder);
                im.OnProgressUpdateEventHandler -= Bgw_ProgressUpdate;
                currentOperation++;
            }
        }

        private void Bgw_ProgressUpdate(object sender, ProgressEventArgs e)
        {
            ProgressBarMaxValue = e.Max;
            ProgressBarValue = e.Progress;
            DetailProgressText = e.Description;
            MainProgressText = "Installation " + e.ModuleDescription + " [" + currentOperation + "/" + numberOfOperation + "]: " + e.Title;
        }

        private void Bgw_Finished(object sender, RunWorkerCompletedEventArgs e)
        {
            CurrentlyDownloading = false;
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
