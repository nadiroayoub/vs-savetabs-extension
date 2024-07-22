using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using System;
using System.Runtime.InteropServices;

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
        /// <summary>
        /// Initializes a new instance of the <see cref="SaveTabsDialog"/> class.
        /// </summary>
        public SaveTabsDialog() : base(null)
        {
            this.Caption = "Save Tabs Tool Window";
            this.Content = new SaveTabsDialogControl((DTE2)ServiceProvider.GlobalProvider.GetService(typeof(DTE)));

        }
    }
}
