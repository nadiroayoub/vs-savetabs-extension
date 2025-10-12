using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using EnvDTE;
using EnvDTE80;
using Newtonsoft.Json;


namespace SaveTabs
{
    /// <summary>
    /// Interaction logic for SaveTabsDialogControl.
    /// </summary>
    public partial class SaveTabsDialogControl : UserControl
    {
        private readonly DTE2 _dte;
        private EnvDTE.DocumentEvents _docEvents;
        private EnvDTE.WindowEvents _winEvents;
        private EnvDTE.SolutionEvents _solEvents;

        private static string RootDir =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SaveTabs");

        private static string Sanitize(string s)
        {
            foreach (var c in Path.GetInvalidFileNameChars()) s = s.Replace(c, '_');
            s = (s ?? string.Empty).Trim();
            return string.IsNullOrWhiteSpace(s) ? "Untitled" : s;
        }

        public void RefreshAll()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            LoadOpenTabs();
            LoadSavedTabLists();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SaveTabsDialogControl"/> class.
        /// </summary>
        public SaveTabsDialogControl(DTE2 dte)
        {
            this.InitializeComponent();
            _dte = dte;

            RefreshAll();

            this.IsVisibleChanged += (_, __) =>
            {
                if (IsVisible)
                    RefreshAll();
            };
        }

        private void SavedListsListBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            DeleteMenuItem.IsEnabled = SavedListsListBox.SelectedItem is string;
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            DeleteSelectedList();
        }

        private void DeleteSelectedList()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (SavedListsListBox.SelectedItem is not string name)
            {
                System.Windows.MessageBox.Show("Select a saved list to delete.", "Save Tabs",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var filePath = Path.Combine(RootDir, $"{Sanitize(name)}.json");
            if (!File.Exists(filePath))
            {
                System.Windows.MessageBox.Show("Saved list not found.", "Save Tabs",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                LoadSavedTabLists();
                return;
            }

            var confirm = System.Windows.MessageBox.Show(
                $"Delete “{name}”?",
                "Delete Saved List",
                MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                File.Delete(filePath);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Could not delete:\n{ex.Message}", "Save Tabs",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            LoadSavedTabLists();
            DeleteMenuItem.IsEnabled = false;
        }

        private void LoadOpenTabs()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var tempDir = Path.GetTempPath();
            var docs = _dte.Documents.Cast<Document>()
                         .Select(d => d.FullName)
                         .Where(p =>
                             !string.IsNullOrWhiteSpace(p) &&
                             File.Exists(p) &&
                             !p.StartsWith(tempDir, StringComparison.OrdinalIgnoreCase)) // <-- تجاهل الملفات المؤقتة
                         .Distinct()
                         .ToList();

            var groups = docs.GroupBy(Path.GetFileName);
            var items = new List<FileItem>();

            foreach (var g in groups)
            {
                if (g.Count() == 1)
                {
                    var p = g.First();
                    items.Add(new FileItem { FullPath = p, DisplayName = Path.GetFileName(p) });
                }
                else
                {
                    foreach (var p in g)
                    {
                        items.Add(new FileItem
                        {
                            FullPath = p,
                            DisplayName = $"{Path.GetFileName(p)} ({Path.GetDirectoryName(p)})"
                        });
                    }
                }
            }

            TabsListBox.ItemsSource = items.OrderBy(i => i.DisplayName).ToList();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var taskName = Sanitize(NameTextBox.Text);
            if (string.IsNullOrWhiteSpace(taskName))
            {
                System.Windows.MessageBox.Show("Please enter a task name.", "Save Tabs");
                return;
            }

            var selected = TabsListBox.SelectedItems.Cast<FileItem>().Select(i => i.FullPath).ToList();
            if (selected.Count == 0)
            {
                selected = _dte.Documents.Cast<Document>()
                             .Select(d => d.FullName)
                             .Where(p => !string.IsNullOrWhiteSpace(p) && File.Exists(p))
                             .Distinct()
                             .ToList();
            }

            Directory.CreateDirectory(RootDir);
            var path = Path.Combine(RootDir, $"{taskName}.json");
            File.WriteAllText(path, JsonConvert.SerializeObject(selected, Formatting.Indented));

            System.Windows.MessageBox.Show("Tabs saved successfully!", "Save Tabs", MessageBoxButton.OK, MessageBoxImage.Information);
            NameTextBox.Clear();
            LoadSavedTabLists();
        }

        private void SaveSelectedTabs(List<string> selectedTabs, string taskName)
        {
            // Implement your save logic here, e.g., save to a file or settings
            Directory.CreateDirectory(RootDir);
            var filePath = Path.Combine(RootDir, $"{taskName}.json");
            var json = JsonConvert.SerializeObject(selectedTabs, Formatting.Indented);
            File.WriteAllText(filePath, json);
        }

        private void LoadSavedTabLists()
        {
            Directory.CreateDirectory(RootDir);
            var files = Directory.GetFiles(RootDir, "*.json")
                                 .Select(Path.GetFileNameWithoutExtension)
                                 .OrderBy(x => x)
                                 .ToList();
            SavedListsListBox.ItemsSource = files;
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            LoadOpenTabs();
        }

        private async void LoadButton_Click(object sender, RoutedEventArgs e)
        {
            if (SavedListsListBox.SelectedItem is not string name)
            {
                System.Windows.MessageBox.Show("Select a saved list.", "Load Tabs");
                return;
            }

            var path = Path.Combine(RootDir, $"{Sanitize(name)}.json");
            if (!File.Exists(path))
            {
                System.Windows.MessageBox.Show("Saved list not found.", "Load Tabs");
                return;
            }

            var tabs = JsonConvert.DeserializeObject<List<string>>(File.ReadAllText(path)) ?? new();

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            int opened = 0;
            foreach (var f in tabs)
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(f) || !File.Exists(f)) continue;

                    var existing = _dte.Documents.Cast<Document>().FirstOrDefault(d => d.FullName == f);
                    if (existing != null) existing.Activate();
                    else _dte.ItemOperations.OpenFile(f);

                    opened++;
                }
                catch { }
            }

            LoadOpenTabs();
            System.Windows.MessageBox.Show($"{opened} tab(s) loaded successfully.", "Load Tabs", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ListBoxItem_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListBoxItem lbi) lbi.IsSelected = true;
        }

        private void SavedListsListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (SavedListsListBox.SelectedItem is string)
                LoadButton_Click(sender, e);
        }

        private void SavedListsListBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
            {
                DeleteButton_Click(sender, e);
                e.Handled = true;
            }
        }

        private void CtxOpenFolder_Click(object sender, RoutedEventArgs e)
        {
            Directory.CreateDirectory(RootDir);
            var psi = new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = RootDir,
                UseShellExecute = true
            };
            System.Diagnostics.Process.Start(psi);
        }

        private void CtxRename_Click(object sender, RoutedEventArgs e)
        {
            if (SavedListsListBox.SelectedItem is not string oldName) return;

            var oldFile = Path.Combine(RootDir, $"{Sanitize(oldName)}.json");
            if (!File.Exists(oldFile))
            {
                System.Windows.MessageBox.Show("Saved list not found.", "Save Tabs");
                LoadSavedTabLists();
                return;
            }

            var input = ShowInputDialog("Rename Saved List", "New name:", oldName);
            if (string.IsNullOrWhiteSpace(input)) return;

            var newName = Sanitize(input);
            var newFile = Path.Combine(RootDir, $"{newName}.json");
            if (File.Exists(newFile))
            {
                System.Windows.MessageBox.Show("A list with that name already exists.", "Save Tabs");
                return;
            }

            try { File.Move(oldFile, newFile); }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Could not rename:\n{ex.Message}", "Save Tabs",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            LoadSavedTabLists();
            SavedListsListBox.SelectedItem = newName;
        }

        private string? ShowInputDialog(string title, string label, string initialText = "")
        {
            var owner = System.Windows.Window.GetWindow(this);

            var win = new System.Windows.Window
            {
                Title = title,
                Owner = owner,
                WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner,
                SizeToContent = System.Windows.SizeToContent.WidthAndHeight,
                ResizeMode = System.Windows.ResizeMode.NoResize,
                WindowStyle = System.Windows.WindowStyle.ToolWindow
            };

            var panel = new StackPanel { Margin = new Thickness(12) };
            panel.Children.Add(new TextBlock { Text = label, Margin = new Thickness(0, 0, 0, 6) });

            var tb = new TextBox { Text = initialText, MinWidth = 280 };
            panel.Children.Add(tb);

            var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 10, 0, 0) };
            var ok = new Button { Content = "OK", IsDefault = true, MinWidth = 80, Margin = new Thickness(0, 0, 8, 0) };
            var cancel = new Button { Content = "Cancel", IsCancel = true, MinWidth = 80 };
            buttons.Children.Add(ok);
            buttons.Children.Add(cancel);
            panel.Children.Add(buttons);

            ok.Click += (_, __) => win.DialogResult = true;
            win.Content = panel;

            return win.ShowDialog() == true ? tb.Text : null;
        }

    }
}