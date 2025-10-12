using Microsoft.VisualStudio.Shell;
using System.Runtime.InteropServices;
using EnvDTE80;
using Microsoft.VisualStudio.Shell.Interop;


namespace SaveTabs
{
    /// <summary>
    /// This class implements the tool window exposed by this package and hosts a user control.
    /// </summary>
    /// <remarks>
    /// In Visual Studio tool windows are composed of a frame (implemented by the shell) and a pane,
    /// usually implemented by the package implementer.
    /// <para>
    /// This class derives from the ToolWindowPane class provided from the MPF in order to use its
    /// implementation of the IVsUIElementPane interface.
    /// </para>
    /// </remarks>
    [Guid("03a2319d-b0a4-49de-bd95-22fac9b719ff")]
    public class SaveTabsDialog : ToolWindowPane
    {
        public SaveTabsDialog() : base(null)
        {
            this.Caption = "Save Tabs Tool";
            var dte = (DTE2)Microsoft.VisualStudio.Shell.Package.GetGlobalService(typeof(SDTE));
            this.Content = new SaveTabsDialogControl(dte);
        }

    }
}
