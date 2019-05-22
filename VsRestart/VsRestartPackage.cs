// -----------------------------------------------------------------------
// <copyright file="VsRestartPackage.cs" company="Equilogic (Pty) Ltd">
//     Copyright © Equilogic (Pty) Ltd. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Equilogic.VisualStudio.VsRestart
{
    using System;
    using System.ComponentModel.Design;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using System.Threading;

    using EnvDTE;

    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Shell.Interop;

    using Process = System.Diagnostics.Process;
    using Task = System.Threading.Tasks.Task;

    /// <summary>
    ///     This is the class that implements the package exposed by this assembly.
    ///     The minimum requirement for a class to be considered a valid package for Visual Studio
    ///     is to implement the IVsPackage interface and register itself with the shell.
    ///     This package uses the helper classes defined inside the Managed Package Framework (MPF)
    ///     to do it: it derives from the Package class that provides the implementation of the
    ///     IVsPackage interface and uses the registration attributes defined in the framework to
    ///     register itself and its components with the shell.
    /// </summary>

    // This attribute tells the PkgDef creation utility (CreatePkgDef.exe) that this class is
    // a package.
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]

    // This attribute is used to register the information needed to show this package
    // in the Help/About dialog of Visual Studio.
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]

    // This attribute is needed to let the shell know that this package exposes some menus.
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid(GuidList.VsRestartPackageId)]
    [ProvideAutoLoad(UIContextGuids.NoSolution, PackageAutoLoadFlags.BackgroundLoad)]
    public sealed class VsRestartPackage : AsyncPackage
    {
        private DTE _dte;

        // ReSharper disable once NotAccessedField.Local
        private DteInitializer _dteInitializer;

        protected override async Task InitializeAsync(CancellationToken cancellationToken,
                                                      IProgress<ServiceProgressData> progress)
        {
            await base.InitializeAsync(cancellationToken, progress);

            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            await InitializeDTEAsync();

            // Add our command handlers for menu (commands must exist in the .vsct file)

            var mcs = await GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;

            if (mcs == null)
            {
                return;
            }
            

            var restartCommandSingleId = new CommandID(GuidList.TopLevelMenuGuid, (int) MenuId.Restart);
            var restartSingleMenuItem = new OleMenuCommand(RestartMenuItemCallback, restartCommandSingleId);
            mcs.AddCommand(restartSingleMenuItem);

            restartSingleMenuItem.BeforeQueryStatus += OnBeforeQueryStatusSingle;

            // Create the command for the menu item.
            var restartElevatedCommandId = new CommandID(GuidList.RestartElevatedGroupGuid, (int) MenuId.RestartAsAdmin);
            var restartElevatedMenuItem = new OleMenuCommand(RestartMenuItemCallback, restartElevatedCommandId);
            mcs.AddCommand(restartElevatedMenuItem);

            restartElevatedMenuItem.BeforeQueryStatus += OnBeforeQueryStatusGroup;

            var restartCommandId = new CommandID(GuidList.RestartElevatedGroupGuid, (int) MenuId.Restart);
            var restartMenuItem = new OleMenuCommand(RestartMenuItemCallback, restartCommandId);
            mcs.AddCommand(restartMenuItem);
        }

        private static void OnBeforeQueryStatusGroup(object sender, EventArgs e)
        {
            // I don't need this next line since this is a lambda.
            // But I just wanted to show that sender is the OleMenuCommand.
            var item = (OleMenuCommand) sender;
            if (ElevationChecker.CanCheckElevation)
            {
                item.Visible = !ElevationChecker.IsElevated(Process.GetCurrentProcess().Handle);
            }
        }

        private static void OnBeforeQueryStatusSingle(object sender, EventArgs e)
        {
            // I don't need this next line since this is a lambda.
            // But I just wanted to show that sender is the OleMenuCommand.
            var item = (OleMenuCommand) sender;
            if (ElevationChecker.CanCheckElevation)
            {
                item.Visible = ElevationChecker.IsElevated(Process.GetCurrentProcess().Handle);
            }
        }

        private async Task InitializeDTEAsync()
        {
            try
            {
                _dte =  await GetServiceAsync(typeof(DTE)) as DTE;
            }
            catch (Exception)
            {
                _dte = null;
            }

            if (_dte == null)
            {
                var shellService = await GetServiceAsync(typeof(IVsShell)) as IVsShell;
                _dteInitializer = new DteInitializer(shellService, InitializeDTEAsync);
            }
            else
            {
                _dteInitializer = null;
            }
        }

        /// <summary>
        ///     This function is the callback used to execute a command when the a menu item is clicked.
        ///     See the Initialize method to see how the menu item is associated to this function using
        ///     the OleMenuCommandService service and the MenuCommand class.
        /// </summary>
        private void RestartMenuItemCallback(object sender, EventArgs e)
        {
            var dte = _dte;

            if (dte == null)
            {
                // Show some error message and return
                return;
            }

            Debug.Assert(dte != null);

            var elevated = ((OleMenuCommand) sender).CommandID.ID == MenuId.RestartAsAdmin;

            new VisualStudioRestart().Restart(dte, elevated);
        }
    }
}