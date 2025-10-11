# VS SaveTabs – Visual Studio extension

Save and restore sets of open documents (tabs) in Visual Studio.  
Great for switching between tasks without losing context.

## ✨ Features
- Save all or selected open tabs to a named list
- Load a saved list (reopens all files, focuses existing ones)
- Ignore temporary VS files automatically
- Manage saved lists: **Load / Delete** (button & `Del` key)
- Auto-refresh tab list when opening the tool window
- Works with light/dark themes

## 📦 Installation
1. Open the solution in **Visual Studio 2022** (Community or higher).
2. Set startup project to the VSIX project (it already is).
3. Press **F5** – this launches an experimental instance of VS with the extension installed.

> To install manually: build the project, then double-click the generated `.vsix` in `bin\Debug` or `bin\Release`.

## 🚀 Usage
- Open the tool: **View → Other Windows → Save Tabs Tool**.
- **Select Tabs to Save**: choose specific files or leave empty to save all real files.
- Enter a **Task Name** and click **Save**.
- Use **Saved Lists** to load or delete entries.
- Press **Refresh** if needed (the tool also refreshes on open).

## 🖼️ UI
- Tool window with three sections:
  1. *Select Tabs to Save* – list of current open tabs (deduped by name, full path on conflicts).
  2. *Task Name* – name under which to save the list.
  3. *Saved Lists* – saved sets; supports **Load** and **Delete**.

## 🏗️ Project structure
