using Microsoft.Win32;
using Newtonsoft.Json;
using System.IO;
using System.Net;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace GrowJo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ProjectDialog? project { get; set; }
        private Recents recents { get; set; } = new Recents();
        private List<DisplayProjectData> displayProjects { get; set; } = new List<DisplayProjectData>();

        public MainWindow()
        {
            InitializeComponent();
            loadRecents();
        }

        private void btnNewProject_Click(object sender, RoutedEventArgs e)
        {
            project = new ProjectDialog();
            project.Show();
            project.OnProjectSaved += projectSaved;
            project.Closed += projectClosed;
        }

        private void btnLoadProject_Click(object sender, RoutedEventArgs e)
        {
            var openDialog = new OpenFileDialog();
            openDialog.Filter = "gjp Files(*.Gjp)| *.Gjp;| All files(*.*) | *.*";
            openDialog.DefaultExt = ".gjp";
            var openResult = openDialog.ShowDialog();
            if (openResult == true)
            {
                project = new ProjectDialog(openDialog.FileName);
                if (recents.RecentFiles == null)
                {
                    recents.RecentFiles = new List<string>();
                }
                if (!recents.RecentFiles.Contains(openDialog.FileName))
                {
                    recents.RecentFiles.Add(openDialog.FileName);
                }
                var json = JsonConvert.SerializeObject(recents);
                File.WriteAllText($"{AppDomain.CurrentDomain.BaseDirectory}Content\\config.json", json);
            }
        }

        private void projectSaved(object? sender, ProjectData e)
        {
           if (recents.RecentFiles == null)
            {
                recents.RecentFiles = new List<string>();
            }
            if (!recents.RecentFiles.Contains(e.Filename!))
            {
                recents.RecentFiles.Add(e.Filename!);
            }
            
            var json = JsonConvert.SerializeObject(recents);
            File.WriteAllText($"{AppDomain.CurrentDomain.BaseDirectory}Content\\config.json", json);
            project!.OnProjectSaved -= projectSaved;
            project.Closed -= projectClosed;
            project.Close();
            lstProjectView.SelectedItem = null;
            updateRecents();

        }

        private void projectClosed(object? sender, EventArgs e)
        {
            project!.OnProjectSaved -= projectSaved;
            project.Closed -= projectClosed;
            lstProjectView.SelectedItem = null;
        }

        private void loadRecents()
        {
            var configFile = $"{AppDomain.CurrentDomain.BaseDirectory}Content\\config.json";
            if (File.Exists(configFile))
            {
                var json = File.ReadAllText(configFile);
                recents = JsonConvert.DeserializeObject<Recents>(json)!;
            }
            updateRecents();

        }

        private void updateRecents()
        {
            if (recents.RecentFiles != null && recents.RecentFiles.Count > 0)
            {
                List<int> indicesToRemove = new List<int>();
                foreach (var recent in recents.RecentFiles)
                {
                    if (File.Exists(recent))
                    {
                        var json = File.ReadAllText(recent);
                        var projectData = JsonConvert.DeserializeObject<ProjectData>(json);
                        var displayProjectData = new DisplayProjectData(projectData!);
                        displayProjectData!.LoadThumbnail();
                        displayProjects.Add(displayProjectData);
                    }
                    else
                    {
                        var index = recents.RecentFiles.IndexOf(recent);
                        indicesToRemove.Add(index);
                    }
                }
                if (indicesToRemove.Count > 0)
                {
                    foreach (var index in indicesToRemove)
                    {
                        recents.RecentFiles.RemoveAt(index);
                    }
                    var json = JsonConvert.SerializeObject(recents);
                    File.WriteAllText($"{AppDomain.CurrentDomain.BaseDirectory}Content\\config.json", json);
                }
                lstProjectView.ItemsSource = displayProjects;
            }

        }

        private void lstProjectView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selected = lstProjectView.SelectedItem as DisplayProjectData;
            if (selected != null)
            {
                //var source = e as DisplayProjectData;
                if (project != null && project.ShowActivated)
                {
                    project.OnProjectSaved -= projectSaved;
                }


                project = new ProjectDialog(selected!.Filename!);
                project.Show();
                project.OnProjectSaved += projectSaved;
                project.Closed += projectClosed;
            }
        }
    }
}