using GrowJo.Helpers;
using GrowJo.Utilities;
using Microsoft.Win32;
using Newtonsoft.Json;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace GrowJo
{
    /// <summary>
    /// Interaction logic for NewProjectDialog.xaml
    /// </summary>
    public partial class ProjectDialog : Window
    {
        public DisplayProjectData ProjectData { get; set; }
        public ObservableCollection<DailyEntrySelectedActions> ActionsList { get; set; } = new ObservableCollection<DailyEntrySelectedActions>();
        public ObservableCollection<ImageData> Images { get; set; } = new ObservableCollection<ImageData>();
        public ObservableCollection<NutrientData> Nutrients { get; set; } = new ObservableCollection<NutrientData>();
        public ObservableCollection<NutrientData> Terpenes { get; set; } = new ObservableCollection<NutrientData>();

        public ImageEditor? ImageEditor { get; set; }

        public event EventHandler<ProjectData>? OnProjectSaved;

        public ProjectDialog()
        {
            InitializeComponent();
            ProjectData = new DisplayProjectData();
            InitializeProject();
        }

        public ProjectDialog(string filename)
        {
            InitializeComponent();
            if (File.Exists(filename))
            {
                var json = File.ReadAllText(filename);
                var projectData = JsonConvert.DeserializeObject<ProjectData>(json);
                ProjectData = new DisplayProjectData(projectData!);
            }
            else
            {
                ProjectData = new DisplayProjectData();
            }
            txtStrain.Text = ProjectData.StrainName;
           
                ProjectData.LoadThumbnail();
                imgThumbnail.Source = ProjectData.ProjectThumbnail;
                InitializeProject();
            if (ProjectData.Medium != null)
            {
                cmbMedium.SelectedItem = ProjectData.Medium;
                if (ProjectData.Medium == GrowMedium.Other)
                {
                    txtOtherMedium.Text = ProjectData.CustomMedium;
                    pnlMediumOther.Visibility = Visibility.Visible;

                }
            }
            if (ProjectData.FinalYield != null)
            {
                txtYield.Text = ProjectData.FinalYield.ToString();
            }
            cmbYieldUnits.SelectedItem = ProjectData.YieldUnits;
            if (ProjectData.Potency != null)
            {
                txtPotency.Text = ProjectData.Potency.ToString();
            }
            if (ProjectData.Terpenes != null && ProjectData.Terpenes.Count > 0)
            {
                foreach (var terpene in ProjectData.Terpenes)
                {
                    Terpenes.Add(new NutrientData
                    {
                        Amount = terpene.Amount,
                        NutrientName = terpene.NutrientName
                    });
                }

            }
            if (!string.IsNullOrWhiteSpace(ProjectData.EffectsDescription))
            {
                txtEffectsSummary.Text = ProjectData.EffectsDescription;
            }

        }

        private void InitializeProject()
        {
            var actions = Enum.GetValues(typeof(Actions));
            foreach (var action in actions)
            {
                lbAvailableActions.Items.Add(action);
            }
            var stages = Enum.GetValues(typeof(Stage));
            cmbEntryStage.ItemsSource = stages;
            cbStage.ItemsSource = stages;
            var units = Enum.GetValues(typeof(MeasurementUnits));
            cmbNutrientUnits.ItemsSource = units;
            cmbYieldUnits.ItemsSource = units;
            lvActions.ItemsSource = ActionsList;
            lvNutrients.ItemsSource = Nutrients;
            lvDailyEntryImageCarousel.ItemsSource = Images;
            var medium = Enum.GetValues(typeof(GrowMedium));
            cmbMedium.ItemsSource = medium;

        }

        private void LoadEntriesList()
        {
            var stages = Enum.GetValues(typeof(Stage)).Cast<Stage>().ToList();
            ProjectData.Entries = ProjectData.Entries!.OrderBy(o => o.Key).ToDictionary();
            foreach (var stage in stages)
            {
                var stagedEntries = ProjectData.Entries!.Values.Where(w => w.State == stage).ToList();
                if (stagedEntries.Count > 0)
                {
                    cmbEntries.Items.Add(stage.ToString());
                    foreach (var entry in ProjectData.Entries)
                    {
                        if (entry.Value.State == stage)
                        {
                            cmbEntries.Items.Add(entry.Key);
                        }
                    }
                }
            }
        }

        private void LoadEntry(DateTime date)
        {
            if (ProjectData.Entries!.ContainsKey(date))
            {
                var entry = ProjectData.Entries[date];

                pnlEntries.Visibility = Visibility.Visible;
                pnlEntries2.Visibility = Visibility.Visible;
                pnlEntries3.Visibility = Visibility.Visible;
                pnlProject.Visibility = Visibility.Collapsed;
                pnlSaveProject.Visibility = Visibility.Collapsed;
                pnlStrainFinalProps.Visibility = Visibility.Collapsed;
                ActionsList.Clear();
                dpDate.SelectedDate = date;
                if (entry.Actions != null && entry.Actions.Count > 0)
                {
                    foreach (var action in entry.Actions)
                    {
                        ActionsList.Add(new DailyEntrySelectedActions
                        {
                            Action = action,

                        });
                    }
                }
                if (entry.CustomActions != null && entry.CustomActions.Count > 0)
                {
                    foreach (var customAction in entry.CustomActions)
                    {
                        ActionsList.Add(new DailyEntrySelectedActions
                        {
                            CustomAction = customAction,
                            IsCustom = true
                        });
                    }
                }
                if (entry.NutrientData != null && entry.NutrientData.Count > 0)
                {
                    foreach (var data in entry.NutrientData)
                    {
                        Nutrients.Add(new NutrientData
                        {
                            Amount = data.Amount,
                            NutrientName = data.NutrientName,
                            Unit = data.Unit
                        });
                    }
                }
                cbStage.SelectedItem = entry.State;
                txtNotes.Text = entry.Notes;
                Images.Clear();
                if (entry.PictureFilenames != null && entry.PictureFilenames.Count > 0)
                {
                    foreach (var imageFilename in entry.PictureFilenames)
                    {
                        if (File.Exists(imageFilename))
                        {
                            ImageData imgData = new ImageData(imageFilename);
                            Images.Add(imgData);
                        }
                    }
                }
            }
        }


        private void SaveProject(string fileName)
        {
            if (!string.IsNullOrWhiteSpace(fileName))
            {
                ProjectData.StrainName = txtStrain.Text;
                ProjectData.Filename = fileName;
                if (cmbMedium.SelectedItem != null)
                {
                    var medium = (GrowMedium)Enum.Parse(typeof(GrowMedium), cmbMedium.SelectedItem.ToString()!);
                    if (medium == GrowMedium.Other && !string.IsNullOrWhiteSpace(txtOtherMedium.Text))
                    {
                        ProjectData.CustomMedium = txtOtherMedium.Text;
                    }
                    ProjectData.Medium = medium;
                }
                if (ValidateYield())
                {
                    float amount = float.Parse(txtYield.Text);
                    ProjectData.FinalYield = amount;
                    var yieldUnits = (MeasurementUnits)Enum.Parse(typeof(MeasurementUnits), cmbYieldUnits.SelectedItem.ToString()!);
                    ProjectData.YieldUnits = yieldUnits;
                }
                if (ValidatePotency())
                {
                    float potency = float.Parse(txtPotency.Text);
                    ProjectData.Potency = potency;
                }
                if (!string.IsNullOrWhiteSpace(txtEffectsSummary.Text))
                {
                    ProjectData.EffectsDescription = txtEffectsSummary.Text;
                }
                


                var projectData = (ProjectData)ProjectData;
                var json = JsonConvert.SerializeObject(projectData);
                File.WriteAllText(fileName, json);
                OnProjectSaved?.Invoke(this, projectData);
                btnSaveProject.IsEnabled = false;
            }
        }

        private bool ValidateEntry()
        {
            if (!dpDate.SelectedDate.HasValue || cbStage.SelectedIndex < 0)
            {
                return false;
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(ProjectData.Filename))
                {
                    btnSaveProjectAs.IsEnabled = true;
                }
            }
                return true;
        }

        private bool ValidateCustomAction()
        {
            return !string.IsNullOrWhiteSpace(txtCustomAction.Text);
        }

        private bool ValidateNutrients()
        {
            if (string.IsNullOrWhiteSpace(txtNumberNutrients.Text) || (string.IsNullOrWhiteSpace(txtNutrientName.Text)))
            {
                return false;
            }
            float numNutrientsAmount = -1;
            if (!float.TryParse(txtNumberNutrients.Text, out numNutrientsAmount))
            {
                return false;
            }
            if (numNutrientsAmount <= 0)
            {
                return false;
            }
            if (cmbNutrientUnits.SelectedItem == null)
            {
                return false;
            }
            return true;
        }

        private bool ValidateYield()
        {
            if (string.IsNullOrWhiteSpace(txtYield.Text) || cmbYieldUnits.SelectedIndex < 0)
            {
                return false;
            }
            if (!float.TryParse(txtYield.Text, out _))
            {
                return false;
            }

            return true;
        }

        private bool ValidatePotency()
        {
            if (string.IsNullOrWhiteSpace(txtPotency.Text))
            {
                return false;
            }
            if (!float.TryParse(txtPotency.Text, out _))
            {
                return false;
            }
            return true;
        }

        private bool ValidateAddTerpenes()
        {
            return true;
        }

        private void ResetEntry()
        {
            ActionsList.Clear();
            Images.Clear();
            Nutrients.Clear();
            txtNotes.Text = string.Empty;
            cbStage.SelectedItem = null;
            pnlEntries.Visibility = Visibility.Collapsed;
            pnlEntries2.Visibility = Visibility.Collapsed;
            pnlEntries3.Visibility = Visibility.Collapsed;
            pnlProject.Visibility = Visibility.Visible;
            pnlSaveProject.Visibility = Visibility.Visible;
            pnlStrainFinalProps.Visibility = Visibility.Visible;
            btnSaveProjectAs.IsEnabled = true;
            txtStrain.IsEnabled = true;
            btnNewEntry.IsEnabled = true;
        }


        private void btnSaveProject_Click(object sender, RoutedEventArgs e)
        {
            SaveProject(ProjectData.Filename!);
        }

        private void btnLoadThumbnail_Click(object sender, RoutedEventArgs e)
        {
            var openDialog = new OpenFileDialog();
            openDialog.Filter = "PNG Files(*.PNG)| *.PNG;| JPG Files(*.JPG)| *.JPG| All files(*.*) | *.*";
            openDialog.DefaultExt = ".png";

            var openResult = openDialog.ShowDialog();
            if (openResult == true)
            {
                ProjectData.ProjectThumbnailFilename = openDialog.FileName;

                    ProjectData.LoadThumbnail();
                    imgThumbnail.Source = ProjectData.ProjectThumbnail;
                
            }
        }

        private void txtStrain_TextChanged(object sender, TextChangedEventArgs e)
        {
            btnSaveProjectAs.IsEnabled = !string.IsNullOrWhiteSpace(txtStrain.Text);
            if (!string.IsNullOrWhiteSpace(ProjectData.Filename))
            {
                btnSaveProject.IsEnabled = true;
            }
        }

        private void cmbEntries_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedItem = cmbEntries.SelectedItem;
            if (selectedItem != null)
            {
                DateTime dateTimePick;
                if (DateTime.TryParse(selectedItem.ToString(), out dateTimePick))
                {
                    //LoadEntry(dateTimePick);
                    btnLoadEntry.IsEnabled = true;
                    btnDeleteEntry.IsEnabled = true;
                }
            }
            else
            {
                btnLoadEntry.IsEnabled = false;
                btnDeleteEntry.IsEnabled = false;
            }
        }

        private void btnNewEntry_Click(object sender, RoutedEventArgs e)
        {
            pnlEntries.Visibility = Visibility.Visible;
            pnlEntries2.Visibility = Visibility.Visible;
            pnlEntries3.Visibility = Visibility.Visible;
            pnlProject.Visibility = Visibility.Collapsed;
            pnlSaveProject.Visibility = Visibility.Collapsed;
            pnlStrainFinalProps.Visibility = Visibility.Collapsed;
            txtStrain.IsEnabled = false;
            btnSaveProjectAs.IsEnabled = false;
            btnNewEntry.IsEnabled = false;
        }

        private void dpDate_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            btnSaveEntry.IsEnabled = ValidateEntry();
        }

        private void btnSaveEntry_Click(object sender, RoutedEventArgs e)
        {
            var stage = (Stage)Enum.Parse(typeof(Stage), cbStage.SelectedValue.ToString()!);
            DailyEntry dailyEntry = new DailyEntry
            {
                Actions = ActionsList.Where(w => !w.IsCustom).Select(s => s.Action!.Value).ToList(),
                CustomActions = ActionsList.Where(w => w.IsCustom).Select(s => s.CustomAction!).ToList(),
                Notes = txtNotes.Text,
                PictureFilenames = Images.Select(s => s.Filename).ToList(),
                State = stage,
                NutrientData = Nutrients.ToList()
            };
            if (ProjectData.Entries == null)
            {
                ProjectData.Entries = new Dictionary<DateTime, DailyEntry>();
            }
            var date = dpDate.SelectedDate;
            if (!ProjectData.Entries.ContainsKey(date!.Value)) {
                ProjectData.Entries.Add(date.Value, dailyEntry);
            }
            else
            {
                ProjectData.Entries[date.Value] = dailyEntry;
            }
                cmbEntries.Items.Clear();
            LoadEntriesList();
            ResetEntry();
            if (!string.IsNullOrWhiteSpace(ProjectData.Filename))
            {
                btnSaveProject.IsEnabled = true;
            }
        }

        private void btnCancelEntry_Click(object sender, RoutedEventArgs e)
        {
            ResetEntry();

        }

        private void btnAddAction_Click(object sender, RoutedEventArgs e)
        {
            if (lbAvailableActions.SelectedItem != null)
            {
                
                Actions selectedAction = (Actions)Enum.Parse(typeof(Actions), lbAvailableActions.SelectedItem.ToString()!, true);
                var newSelectedAction = new DailyEntrySelectedActions
                {
                    Action = selectedAction
                };
                if (!ActionsList.Contains(newSelectedAction))
                {
                    ActionsList.Add(newSelectedAction);
                }
            }
        }

        private void ActionCancelButton_Click(object sender, RoutedEventArgs e)
        {
            Button? button = sender as Button;
            var context = button!.DataContext as DailyEntrySelectedActions;
            if (context != null)
            {
                ActionsList.Remove(context);
            }
        }

        private void NutrientCancelButton_Click(object sender, RoutedEventArgs e)
        {
            Button? button = sender as Button;
            var context = button!.DataContext as NutrientData;
            if (context != null)
            {
                Nutrients.Remove(context);
            }
        }

        private void btnAddImages_Click(object sender, RoutedEventArgs e)
        {
            var openDialog = new OpenFileDialog();
            openDialog.Filter = "PNG Files(*.PNG)| *.PNG;| JPG Files(*.JPG)| *.JPG| All files(*.*) | *.*";
            openDialog.DefaultExt = ".png";
            openDialog.Multiselect = true;
            var openResult = openDialog.ShowDialog();
            if (openResult == true)
            {
                if (openDialog.FileNames != null && openDialog.FileNames.Length > 0)
                {
                    foreach (var file in openDialog.FileNames)
                    {
                        ImageData imageData = new ImageData(file);
                        Images.Add(imageData);
                    }
                }
            }
        }

        private void lvDailyEntryImageCarousel_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void cbStage_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            btnSaveEntry.IsEnabled = ValidateEntry();
        }

        private void btnAddCustomAction_Click(object sender, RoutedEventArgs e)
        {
            pnlCustomAction.Visibility = Visibility.Visible;
            btnAddCustomAction.IsEnabled = false;
            btnCancelCustomAction.IsEnabled = true;
        }

        private void txtCustomAction_TextChanged(object sender, TextChangedEventArgs e)
        {
            btnSaveCustomAction.IsEnabled = ValidateCustomAction();
        }

        private void btnSaveCustomAction_Click(object sender, RoutedEventArgs e)
        {
            var customAction = new DailyEntrySelectedActions
            {
                IsCustom = true,
                CustomAction = txtCustomAction.Text
            };
            if (!ActionsList.Contains(customAction))
            {
                ActionsList.Add(customAction);
            }
            pnlCustomAction.Visibility = Visibility.Collapsed;
            txtCustomAction.Text = string.Empty;
            btnAddCustomAction.IsEnabled = true;
            btnCancelCustomAction.IsEnabled = false;
        }

        private void btnCancelCustomAction_Click(object sender, RoutedEventArgs e)
        {
            txtCustomAction.Text = string.Empty;
            pnlCustomAction.Visibility= Visibility.Collapsed;
            btnCancelCustomAction.IsEnabled = false;
            btnAddCustomAction.IsEnabled = true;
        }

        private void txtNumberNutrients_TextChanged(object sender, TextChangedEventArgs e)
        {
            btnAddNutrients.IsEnabled = ValidateNutrients();
        }

        private void cmbNutrientUnits_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            btnAddNutrients.IsEnabled = ValidateNutrients();
        }

        private void txtNutrientName_SelectionChanged(object sender, RoutedEventArgs e)
        {
            btnAddNutrients.IsEnabled = ValidateNutrients();
        }

        private void btnAddNutrients_Click(object sender, RoutedEventArgs e)
        {
            float amount = float.Parse(txtNumberNutrients.Text);
            MeasurementUnits units = (MeasurementUnits)Enum.Parse(typeof(MeasurementUnits), cmbNutrientUnits.SelectedItem.ToString()!, true);

            NutrientData nutrient = new NutrientData { 
                Amount = amount,
                NutrientName = txtNutrientName.Text,
                Unit = units
            };

            Nutrients.Add(nutrient);
            txtNutrientName.Text = string.Empty;
            txtNumberNutrients.Text = string.Empty;
            cmbNutrientUnits.SelectedItem = null;
        }

        private void cmbMedium_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbMedium.SelectedItem != null && cmbMedium.SelectedItem.ToString()!.ToUpper() == "OTHER")
            {
                pnlMediumOther.Visibility = Visibility.Visible;
            }
            else
            {
                pnlMediumOther.Visibility = Visibility.Collapsed;
                txtOtherMedium.Text = string.Empty;
                ProjectData.CustomMedium = null;
            }
            if (!string.IsNullOrWhiteSpace(ProjectData.Filename))
            {
                btnSaveProject.IsEnabled = true;
            }
        }

        private void txtOtherMedium_TextChanged(object sender, TextChangedEventArgs e)
        {
                btnSubmitOtherMedium.IsEnabled = !string.IsNullOrWhiteSpace (txtOtherMedium.Text);   
        }

        private void btnSubmitOtherMedium_Click(object sender, RoutedEventArgs e)
        {
            ProjectData.CustomMedium = txtOtherMedium.Text;
            ProjectData.Medium = null;
            if (!string.IsNullOrWhiteSpace(ProjectData.Filename))
            {
                btnSaveProject.IsEnabled = true;
            }
        }

        private void cmImgThumbnailEdit_Click(object? sender, RoutedEventArgs e)
        {
            ImageEditor = new ImageEditor(ProjectData.ProjectThumbnailFilename!);
            ImageEditor.Show();
            ImageEditor.Closed += UpdateThumbnail;
            this.IsEnabled = false;

        }

        private void cmLvImageCarouselEdit_Click(object sender, RoutedEventArgs e)
        {
            var selectedItem = lvDailyEntryImageCarousel.SelectedItem as ImageData;
            ImageEditor = new ImageEditor(selectedItem!.Filename);
            ImageEditor.Show();
            ImageEditor.Closed += UpdateCarousel;
            this.IsEnabled = false;
        }

        private void cmLvImageCarouselRemove_Click(object sender, RoutedEventArgs e)
        {

        }

        private void UpdateThumbnail(object? sender, EventArgs e)
        {
            this.IsEnabled = true;
            ProjectData.LoadThumbnail();
            if (!string.IsNullOrWhiteSpace(ProjectData.Filename)) {
                btnSaveProject.IsEnabled = true;
            }
        }

        private void UpdateCarousel(object? sender, EventArgs e)
        {
            this.IsEnabled = true;
            var date = dpDate.SelectedDate;
            if (date.HasValue)
            {
                for (int i = 0; i < Images.Count; i++)
                {
                    var filename = Images[i].Filename;
                    if (File.Exists(filename))
                    {
                        ImageData img = new ImageData(filename);
                        Images[i] = img;
                    }
                }
            }
        }

        private void cmbEntryStage_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            Stage stage = (Stage)Enum.Parse(typeof(Stage), cmbEntryStage.SelectedItem.ToString()!, true);
            var nextStage = stage.Next();

                var stagedEntries = ProjectData.Entries!.Values.Where(w => w.State == stage).ToList();
            var nextStageFirstEntry = ProjectData.Entries!.Values.Where(w => w.State == nextStage).FirstOrDefault();
            cmbEntries.Items.Clear();
            if (stagedEntries.Count > 0)
            {


                pnlStageLengthInfo.Visibility = Visibility.Visible;
                var keys = new List<DateTime>();
                foreach (var entry in ProjectData.Entries)
                {
                    if (entry.Value.State == stage)
                    {
                        cmbEntries.Items.Add(entry.Key);
                        keys.Add(entry.Key);
                    }
                    else if (entry.Value.State == nextStage)
                    {
                        keys.Add(entry.Key);
                        break;
                    }
                }
                var firstDate = keys.Min();
                var lastDate = keys.Max();
                var days = (lastDate - firstDate).Days;
                var weeks = (lastDate - firstDate).Days / 7;
                lblStageDays.Content = days;
                lblStageWeeks.Content = weeks;
            }
            else
            {
                pnlStageLengthInfo.Visibility = Visibility.Collapsed;
            }
            
        }

        private void btnDeleteEntry_Click(object sender, RoutedEventArgs e)
        {
            var selectedItem = cmbEntries.SelectedItem;
            DateTime dateTimePick;
            if (DateTime.TryParse(selectedItem.ToString(), out dateTimePick))
            {
                if (ProjectData.Entries != null && ProjectData.Entries.Count > 0 && ProjectData.Entries.ContainsKey(dateTimePick))
                {
                    ProjectData.Entries.Remove(dateTimePick);
                }
                LoadEntriesList();
                cmbEntries.SelectedIndex = -1;
                if (!string.IsNullOrWhiteSpace(ProjectData.Filename))
                {
                    btnSaveProject.IsEnabled = true;
                }
            }
        }
        
        private void btnTerpenesCancel_Click(object sender, RoutedEventArgs e)
        {
            Button? button = sender as Button;
            var context = button!.DataContext as NutrientData;
            if (context != null)
            {
                Terpenes.Remove(context);
            }
        }

        private void txtTerpenType_TextChanged(object sender, TextChangedEventArgs e)
        {
            btnAddTerpenes.IsEnabled = ValidateAddTerpenes();
        }

        private void txtTerpeneAmount_TextChanged(object sender, TextChangedEventArgs e)
        {
            btnAddTerpenes.IsEnabled = ValidateAddTerpenes();
        }

        private void btnAddTerpenes_Click(object sender, RoutedEventArgs e)
        {
            float amount = float.Parse(txtTerpeneAmount.Text);
            //MeasurementUnits units = (MeasurementUnits)Enum.Parse(typeof(MeasurementUnits), cmbNutrientUnits.SelectedItem.ToString()!, true);

            NutrientData terpene = new NutrientData
            {
                Amount = amount,
                NutrientName = txtTerpenType.Text,
                Unit = MeasurementUnits.Tsp
            };

            Terpenes.Add(terpene);
            txtTerpeneAmount.Text = string.Empty;
            txtTerpenType.Text = string.Empty;
        }

        private void btnSaveProjectAs_Click(object sender, RoutedEventArgs e)
        {
            var saveDialog = new SaveFileDialog();
            saveDialog.Filter = "gjp Files(*.Gjp)| *.Gjp;| All files(*.*) | *.*";
            saveDialog.DefaultExt = ".gjp";
            var saveResult = saveDialog.ShowDialog();
            if (saveResult == true)
            {
                SaveProject(saveDialog.FileName);
            }
        }

        private void btnLoadEntry_Click(object sender, RoutedEventArgs e)
        {
            var selectedItem = cmbEntries.SelectedItem;
            var dateTime = DateTime.Parse(selectedItem.ToString()!);
            LoadEntry(dateTime);
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (pnlEntries.Visibility == Visibility.Visible)
            {
                e.Cancel = true;
                ResetEntry();
            }
            else
            {
                base.OnClosing(e);
            }
        }
    }
}
