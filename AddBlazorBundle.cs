using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows; // For MessageBox if needed
using Task = System.Threading.Tasks.Task;


namespace BlazorCodeBehindUtil
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class AddBlazorBundle
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 0x0100;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("9ca5e9f7-883f-40ff-ae70-8d2ce136ad25");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly AsyncPackage package;

        /// <summary>
        /// Initializes a new instance of the <see cref="AddBlazorBundle"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="commandService">Command service to add command to, not null.</param>
        private AddBlazorBundle(AsyncPackage package, OleMenuCommandService commandService)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuItem = new MenuCommand(this.Execute, menuCommandID);
            commandService.AddCommand(menuItem);
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static AddBlazorBundle Instance
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private Microsoft.VisualStudio.Shell.IAsyncServiceProvider ServiceProvider
        {
            get
            {
                return this.package;
            }
        }

        private DTE2 _dte;
        private ProjectItemsEvents _projectItemsEvents;

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static async Task InitializeAsync(AsyncPackage package)
        {
            // Switch to the main thread - the call to AddCommand in Command1's constructor requires
            // the UI thread.
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService =
                await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;

            Instance = new AddBlazorBundle(package, commandService);

            // Hook rename watcher
            Instance._dte = (DTE2)await package.GetServiceAsync(typeof(SDTE));

            Events2 events2 = (Events2)Instance._dte.Events;

            Instance._projectItemsEvents = events2.ProjectItemsEvents;

            Instance._projectItemsEvents.ItemRenamed += Instance.OnItemRenamed;
        }

        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// See the constructor to see how the menu item is associated with this function using
        /// OleMenuCommandService service and MenuCommand class.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var dte = (DTE2)Package.GetGlobalService(typeof(SDTE));

            // 1. Get Settings
            var options = (GeneralOptions)package.GetDialogPage(typeof(GeneralOptions));

            // 2. Get Selection
            UIHierarchyItem selectedItem = ((Array)dte.ToolWindows.SolutionExplorer.SelectedItems).Cast<UIHierarchyItem>().First();
            ProjectItem folder = selectedItem.Object as ProjectItem;
            Project project = folder.ContainingProject;
            string folderPath = folder.Properties.Item("FullPath").Value.ToString();

            // 3. Detect Namespace
            string projectNamespace = project.Properties.Item("DefaultNamespace").Value.ToString();
            string projectFolder = Path.GetDirectoryName(project.FullName);
            string relativePath = folderPath.Replace(projectFolder, "").Trim(Path.DirectorySeparatorChar);
            string finalNamespace = projectNamespace + (string.IsNullOrEmpty(relativePath) ? "" : "." + relativePath.Replace(Path.DirectorySeparatorChar, '.'));

            // 4. Show Dialog
            var dialog = new BundleDialog("MyComponent", options.CreateCS, options.CreateCSS, options.CreateJS);
            if (dialog.ShowDialog() != true || string.IsNullOrWhiteSpace(dialog.ComponentName)) return;

            string name = dialog.ComponentName.Trim();

            // --- START OF FIX ---
            // 5. Generate the Namespace Content
            // We only need this if the user checked the .cs box
            string nsContent = "";
            if (dialog.CreateCS)
            {
                if (options.UseFileScopedNamespace)
                {
                    nsContent = $"namespace {finalNamespace};\n\npublic partial class {name}\n{{\n\n}}";
                }
                else
                {
                    nsContent = $"namespace {finalNamespace}\n{{\n    public partial class {name}\n    {{\n    }}\n}}";
                }
            }

            // 6. Define the files we want to create
            var filesToCreate = new Dictionary<string, string>();
            filesToCreate.Add($"{name}.razor", $"<h3>{name}</h3>");

            if (dialog.CreateCS) filesToCreate.Add($"{name}.razor.cs", nsContent);
            if (dialog.CreateCSS) filesToCreate.Add($"{name}.razor.css", "/* Scoped CSS */");
            if (dialog.CreateJS) filesToCreate.Add($"{name}.razor.js", "export function init() { }");
            // --- END OF FIX ---

            // 7. Check for existing files (Safety Check)
            var existingFiles = filesToCreate.Keys
                .Where(fileName => File.Exists(Path.Combine(folderPath, fileName)))
                .ToList();

            if (existingFiles.Any())
            {
                string message = $"The following files already exist:\n\n" +
                                 $"{string.Join("\n", existingFiles)}\n\n" +
                                 "Do you want to overwrite them?";

                var result = System.Windows.Forms.MessageBox.Show(
                    message,
                    "File Collision",
                    System.Windows.Forms.MessageBoxButtons.YesNo,
                    System.Windows.Forms.MessageBoxIcon.Warning);

                if (result == System.Windows.Forms.DialogResult.No) return;
            }

            // 8. Create and Add Files
            foreach (var file in filesToCreate)
            {
                string fullPath = Path.Combine(folderPath, file.Key);
                File.WriteAllText(fullPath, file.Value);
                folder.ProjectItems.AddFromFile(fullPath);
            }
        }

        private void OnDocumentSaved(Document document)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            string filePath = document.FullName;

            if (!filePath.EndsWith(".razor", StringComparison.OrdinalIgnoreCase))
                return;

            string directory = Path.GetDirectoryName(filePath);
            string newBaseName = Path.GetFileNameWithoutExtension(filePath);

            string[] extensions =
                    {
                ".razor.cs",
                ".razor.css",
                ".razor.js"
            };

            foreach (var ext in extensions)
            {
                string oldPattern = Directory
                    .GetFiles(directory, "*" + ext)
                    .FirstOrDefault(f =>
                        Path.GetFileName(f).StartsWith(newBaseName) == false);

                // Instead, better approach:
                string oldFile = Path.Combine(directory, newBaseName + ext);

                if (!File.Exists(oldFile))
                {
                    // Try to detect a file with previous name
                    var match = Directory.GetFiles(directory, "*" + ext)
                        .FirstOrDefault(f => !f.StartsWith(newBaseName));

                    if (match != null)
                    {
                        string newFilePath = Path.Combine(directory, newBaseName + ext);
                        File.Move(match, newFilePath);
                    }
                }
            }
        }

        private void OnItemRenamed(ProjectItem projectItem, string oldName)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (projectItem == null)
                return;

            string newFilePath = projectItem.FileNames[1];

            if (!newFilePath.EndsWith(".razor", StringComparison.OrdinalIgnoreCase))
                return;

            string directory = Path.GetDirectoryName(newFilePath);
            string newBaseName = Path.GetFileNameWithoutExtension(newFilePath);

            string oldBaseName = Path.GetFileNameWithoutExtension(oldName);

            string[] extensions =
            {
                ".razor.cs",
                ".razor.css",
                ".razor.js"
            };

            foreach (var ext in extensions)
            {
                string oldSibling = Path.Combine(directory, oldBaseName + ext);
                string newSibling = Path.Combine(directory, newBaseName + ext);

                if (File.Exists(oldSibling))
                {
                    try
                    {
                        File.Move(oldSibling, newSibling);
                    }
                    catch
                    {
                        // swallow silently or log if desired
                    }
                }
            }
        }
    }
}
