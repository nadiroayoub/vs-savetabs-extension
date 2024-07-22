using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Newtonsoft.Json;
using SaveTabs;



namespace SaveTabsExtension
{
    internal sealed class MyCommand
    {
        public const int CommandId = PackageIds.MyCommand;
        public static readonly Guid CommandSet = new Guid(PackageGuids.SaveTabsExtensionString);
        private readonly AsyncPackage package;

        private MyCommand(AsyncPackage package, OleMenuCommandService commandService)
        {
            this.package = package;

            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuItem = new MenuCommand(this.Execute, menuCommandID);
            commandService.AddCommand(menuItem);
        }

        public static async Task InitializeAsync(AsyncPackage package)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);
            OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            new MyCommand(package, commandService);
        }

        private async void Execute(object sender, EventArgs e)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            var dte = (EnvDTE80.DTE2)await package.GetServiceAsync(typeof(EnvDTE.DTE));
            var tabManager = new TabManager(dte, package);
            await tabManager.SaveTabsAsync("default_task");
            await tabManager.LoadTabsAsync("default_task");
        }
    }


    public class TabManager
    {
        private readonly DTE2 _dte;
        private readonly IServiceProvider _serviceProvider;
        private const string StoragePath = "C:\\TabLayouts"; // Adjust path as needed

        public TabManager(DTE2 dte, IServiceProvider serviceProvider)
        {
            _dte = dte;
            _serviceProvider = serviceProvider;
        }

        public async Task SaveTabsAsync(string taskName)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            Directory.CreateDirectory(StoragePath);
            var documents = GetOpenDocuments();
            var tabInfoList = documents.Select(doc => new TabInfo
            {
                FilePath = doc.FullName,
                IsDirty = !doc.Saved
            }).ToList();

            var json = JsonConvert.SerializeObject(tabInfoList, Formatting.Indented);
            var filePath = Path.Combine(StoragePath, $"{taskName}.json");

            using (var writer = new StreamWriter(filePath))
            {
                await writer.WriteAsync(json);
            }
        }

        public async Task LoadTabsAsync(string taskName)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var filePath = Path.Combine(StoragePath, $"{taskName}.json");

            if (!File.Exists(filePath))
            {
                VsShellUtilities.ShowMessageBox(
                    _serviceProvider,
                    "No saved tabs for this task.",
                    "LoadTabs",
                    OLEMSGICON.OLEMSGICON_WARNING,
                    OLEMSGBUTTON.OLEMSGBUTTON_OK,
                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                return;
            }

            string json;
            using (var reader = new StreamReader(filePath))
            {
                json = await reader.ReadToEndAsync();
            }

            var tabInfoList = JsonConvert.DeserializeObject<List<TabInfo>>(json);
            CloseAllOpenDocuments();

            foreach (var tabInfo in tabInfoList)
            {
                var window = _dte.ItemOperations.OpenFile(tabInfo.FilePath);
                var doc = window.Document;
                if (tabInfo.IsDirty && doc.Saved)
                {
                    doc.Saved = false;
                }
            }

        }

        private IEnumerable<Document> GetOpenDocuments()
        {
            var documents = new List<Document>();
            for (var i = 1; i <= _dte.Documents.Count; i++)
            {
                documents.Add(_dte.Documents.Item(i));
            }
            return documents;
        }

        private void CloseAllOpenDocuments()
        {
            for (var i = _dte.Documents.Count; i > 0; i--)
            {
                _dte.Documents.Item(i).Close(vsSaveChanges.vsSaveChangesYes);
            }
        }

        private class TabInfo
        {
            public string FilePath { get; set; }
            public bool IsDirty { get; set; }
        }
    }


}
