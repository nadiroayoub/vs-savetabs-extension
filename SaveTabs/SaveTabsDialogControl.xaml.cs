using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Newtonsoft.Json;


namespace SaveTabs
{
    /// <summary>
    /// Interaction logic for SaveTabsDialogControl.
    /// </summary>
    public partial class SaveTabsDialogControl : UserControl
    {
        private readonly DTE2 _dte;
        private const string StoragePath = "C:\\TabLayouts"; // Adjust path as needed

        /// <summary>
        /// Initializes a new instance of the <see cref="SaveTabsDialogControl"/> class.
        /// </summary>
        public SaveTabsDialogControl(DTE2 dte)
        {
            this.InitializeComponent();
            _dte = dte;
            LoadOpenTabs();
            LoadSavedTabLists();
        }

        private void LoadOpenTabs()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var documents = _dte.Documents;
            var fileItems = new List<FileItem>();
            var nameCounts = new Dictionary<string, int>();

            foreach (Document doc in documents)
            {
                var fullPath = doc.FullName;
                var fileName = Path.GetFileName(fullPath);

                if (nameCounts.ContainsKey(fileName))
                {
                    nameCounts[fileName]++;
                    fileName = $"{fileName} ({Path.GetDirectoryName(fullPath)})";
                }
                else
                {
                    nameCounts[fileName] = 1;
                }

                fileItems.Add(new FileItem { FullPath = fullPath, DisplayName = fileName });
            }

            TabsListBox.ItemsSource = fileItems;

        }


        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedTabs = new List<string>();
            foreach (FileItem item in TabsListBox.SelectedItems)
            {
                selectedTabs.Add(item.FullPath);
            }

            var taskName = NameTextBox.Text;
            SaveSelectedTabs(selectedTabs, taskName);
            System.Windows.MessageBox.Show("Tabs saved successfully!", "Save Tabs", MessageBoxButton.OK, MessageBoxImage.Information);

            // Clear the NameTextBox after saving
            NameTextBox.Clear();
            LoadSavedTabLists(); // Refresh the list after saving

        }

        private void SaveSelectedTabs(List<string> selectedTabs, string taskName)
        {
            // Implement your save logic here, e.g., save to a file or settings
            Directory.CreateDirectory(StoragePath);
            var filePath = Path.Combine(StoragePath, $"{taskName}.json");
            var json = JsonConvert.SerializeObject(selectedTabs, Formatting.Indented);
            File.WriteAllText(filePath, json);
        }

        private void LoadSavedTabLists()
        {
            SavedListsListBox.Items.Clear();
            if (Directory.Exists(StoragePath))
            {
                var files = Directory.GetFiles(StoragePath, "*.json");
                foreach (var file in files)
                {
                    SavedListsListBox.Items.Add(Path.GetFileNameWithoutExtension(file));
                }
            }
        }

        private void LoadButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedListName = (string)SavedListsListBox.SelectedItem;
            if (selectedListName != null)
            {
                var filePath = Path.Combine(StoragePath, $"{selectedListName}.json");
                var json = File.ReadAllText(filePath);
                var tabs = JsonConvert.DeserializeObject<List<string>>(json);

                ThreadHelper.ThrowIfNotOnUIThread();

                foreach (var tab in tabs)
                {
                    var doc = _dte.Documents.Cast<Document>().FirstOrDefault(d => d.FullName == tab);
                    if (doc != null)
                    {
                        // Document is already open, activate it
                        doc.Activate();
                    }
                    else
                    {
                        // Document is not open, open it
                        _dte.ItemOperations.OpenFile(tab);
                    }
                }

                System.Windows.MessageBox.Show("Tabs loaded successfully!", "Load Tabs", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }
}