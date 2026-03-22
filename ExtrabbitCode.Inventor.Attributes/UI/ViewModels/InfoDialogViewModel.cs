using System.ComponentModel;
using System.Reflection;
using File = System.IO.File;
using Path = System.IO.Path;

namespace ExtrabbitCode.Inventor.Attributes.UI.ViewModels
{
    public class InfoDialogViewModel : INotifyPropertyChanged
    {
        private string _programVersion = string.Empty;
        private string _versionHistory = string.Empty;

        public string ProgramVersion {
            get => _programVersion;
            set {
                _programVersion = value;
                OnPropertyChanged(nameof(ProgramVersion));
            }
        }

        public string VersionHistory {
            get => _versionHistory;
            set {
                _versionHistory = value;
                OnPropertyChanged(nameof(VersionHistory));
            }
        }

        public InfoDialogViewModel()
        {
            LoadProgramVersion();
            LoadVersionHistory();
        }

        private void LoadProgramVersion()
        {
            ProgramVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "Unknown";
        }

        private void LoadVersionHistory()
        {
            string changeLogPath = Path.Combine(
                Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty,
                "Resources",
                "versionhistory.txt");

            VersionHistory = File.Exists(changeLogPath) ? File.ReadAllText(changeLogPath) : "Changelog file not found.";
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}