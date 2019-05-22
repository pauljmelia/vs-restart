// -----------------------------------------------------------------------
// <copyright file="VisualStudioRestart.cs" company="Equilogic (Pty) Ltd">
//     Copyright © Equilogic (Pty) Ltd. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMethodReturnValue.Local
namespace Equilogic.VisualStudio.VsRestart
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Linq;
    using System.Text;

    using Arguments;

    using EnvDTE;

    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Shell.Interop;

    using Process = System.Diagnostics.Process;

    internal class VisualStudioRestart
    {
        private enum ProcessStartResult
        {
            Ok,
            AuthDenied,
            Exception
        }

        internal void Restart(DTE dte, bool elevated)
        {
            var currentProcess = Process.GetCurrentProcess();

            var parser = new ArgumentParser(dte.CommandLineArguments);

            if (currentProcess.MainModule == null)
            {
                return;
            }

            var builder = new RestartProcessBuilder()
                          .WithDevenv(currentProcess.MainModule.FileName).WithArguments(parser.GetArguments());

            var openedItem = GetOpenedItem(dte);
            if (openedItem != OpenedItem.None)
            {
                if (openedItem.IsSolution)
                {
                    builder.WithSolution(openedItem.Name);
                }
                else
                {
                    builder.WithProject(openedItem.Name);
                }
            }

            if (elevated)
            {
                builder.WithElevatedPermission();
            }

            const string commandName = "File.Exit";
            var closeCommand = dte.Commands.Item(commandName);

            CommandEvents closeCommandEvents = null;
            if (closeCommand != null)
            {
                closeCommandEvents = dte.Events.CommandEvents[closeCommand.Guid, closeCommand.ID];
            }

            // Install the handler
            // ReSharper disable once UnusedVariable
            var handler = new VisualStudioEventHandler(dte.Events.DTEEvents, closeCommandEvents, builder.Build());

            if (closeCommand != null && closeCommand.IsAvailable)
            {
                // if the Exit command is present, execute it with all graceful dialogs by VS
                dte.ExecuteCommand(commandName);
            }
            else
            {
                // Close brutally
                dte.Quit();
            }
        }

        private static OpenedItem GetOpenedItem(DTE dte)
        {
            if (dte.Solution != null && dte.Solution.IsOpen)
            {
                if (string.IsNullOrEmpty(dte.Solution.FullName))
                {
                    var activeProjects = (Array) dte.ActiveSolutionProjects;
                    if (activeProjects != null && activeProjects.Length > 0)
                    {
                        var currentOpenedProject = (Project) activeProjects.GetValue(0);
                        if (currentOpenedProject != null)
                        {
                            return new OpenedItem(currentOpenedProject.FullName, false);
                        }
                    }
                }
                else
                {
                    return new OpenedItem(dte.Solution.FullName, true);
                }
            }

            return OpenedItem.None;
        }

        private class OpenedItem
        {
            public static readonly OpenedItem None = new OpenedItem(null, false);

            public OpenedItem(string name, bool isSolution)
            {
                Name = name;
                IsSolution = isSolution;
            }

            public bool IsSolution { get; }

            public string Name { get; }
        }

        private class RestartProcessBuilder
        {
            private ArgumentTokenCollection _arguments;
            private string _devenv;
            private string _projectFile;
            private string _solutionFile;
            private string _verb;

            public ProcessStartInfo Build()
            {
                return new ProcessStartInfo
                       {
                           FileName = _devenv,
                           ErrorDialog = true,
                           UseShellExecute = true,
                           Verb = _verb,
                           Arguments = BuildArguments()
                       };
            }

            public RestartProcessBuilder WithArguments(ArgumentTokenCollection arguments)
            {
                _arguments = arguments;
                return this;
            }

            public RestartProcessBuilder WithDevenv(string devenv)
            {
                _devenv = devenv;
                return this;
            }

            public RestartProcessBuilder WithElevatedPermission()
            {
                _verb = "runas";
                return this;
            }

            public RestartProcessBuilder WithProject(string projectFile)
            {
                _projectFile = projectFile;
                return this;
            }

            public RestartProcessBuilder WithSolution(string solutionFile)
            {
                _solutionFile = solutionFile;
                return this;
            }

            private string BuildArguments()
            {
                if (!string.IsNullOrEmpty(_solutionFile))
                {
                    if (_arguments.OfType<SolutionArgumentToken>().Any())
                    {
                        _arguments.Replace<SolutionArgumentToken>(new SolutionArgumentToken(Quote(_solutionFile)));
                    }
                    else if (_arguments.OfType<ProjectArgumentToken>().Any())
                    {
                        _arguments.Replace<ProjectArgumentToken>(new SolutionArgumentToken(Quote(_solutionFile)));
                    }
                    else
                    {
                        _arguments.Add(new SolutionArgumentToken(Quote(_solutionFile)));
                    }
                }

                if (!string.IsNullOrEmpty(_projectFile))
                {
                    if (_arguments.OfType<SolutionArgumentToken>().Any())
                    {
                        _arguments.Replace<SolutionArgumentToken>(new ProjectArgumentToken(Quote(_projectFile)));
                    }
                    else if (_arguments.OfType<ProjectArgumentToken>().Any())
                    {
                        _arguments.Replace<ProjectArgumentToken>(new ProjectArgumentToken(Quote(_projectFile)));
                    }
                    else
                    {
                        _arguments.Add(new ProjectArgumentToken(Quote(_projectFile)));
                    }
                }

                var escapedArguments = _arguments.ToString().ReplaceSmart(Quote(_devenv), string.Empty);

                return escapedArguments;
            }

            private static string Quote(string input) => $"\"{input}\"";
        }

        private class VisualStudioEventHandler
        {
            private readonly CommandEvents _closeCommandEvents;
            private readonly DTEEvents _dTEEvents;
            private readonly ProcessStartInfo _startInfo;

            public VisualStudioEventHandler(DTEEvents dTEEvents,
                                            CommandEvents closeCommandEvents,
                                            ProcessStartInfo processStartInfo)
            {
                _dTEEvents = dTEEvents;
                _closeCommandEvents = closeCommandEvents;
                _startInfo = processStartInfo;

                dTEEvents.OnBeginShutdown += DTEEvents_OnBeginShutdown;
                if (closeCommandEvents != null)
                {
                    closeCommandEvents.AfterExecute += CommandEvents_AfterExecute;
                }
            }

            private static void DisplayError(Exception ex, ProcessStartResult status)
            {
                if (!(Package.GetGlobalService(typeof(SVsGeneralOutputWindowPane)) is IVsOutputWindowPane outputPane))
                {
                    return;
                }

                outputPane.Activate();

                if (status == ProcessStartResult.AuthDenied)
                {
                    outputPane.OutputString(
                        "Visual Studio restart operation was cancelled by the user." + Environment.NewLine);
                }
                else
                {
                    var sb = new StringBuilder();
                    sb.AppendLine(
                        "An exceptions has been thrown while trying to start an elevated Visual Studio, see details below.");
                    sb.AppendLine(ex.ToString());

                    var diagnostics = sb.ToString();

                    outputPane.OutputString(diagnostics);
                    var log = Package.GetGlobalService(typeof(SVsActivityLog)) as IVsActivityLog;
                    log?.LogEntry((uint) __ACTIVITYLOG_ENTRYTYPE.ALE_ERROR, "Equilogic.VsRestart", diagnostics);
                }

                //EnvDTE.OutputWindow.OutputWindow.Parent.Activate();
            }

            private static ProcessStartResult StartProcessSafe(ProcessStartInfo startInfo,
                                                               Action<Exception, ProcessStartResult> exceptionHandler)
            {
                var result = ProcessStartResult.Ok;

                try
                {
                    Process.Start(startInfo);
                }
                catch (Exception ex)
                {
                    result = ProcessStartResult.Exception;

                    // User has denied auth through UAC
                    if (ex is Win32Exception winex && winex.NativeErrorCode == 1223)
                    {
                        result = ProcessStartResult.AuthDenied;
                    }

                    exceptionHandler(ex, result);
                }

                return result;
            }

            private void CommandEvents_AfterExecute(string guid, int id, object customIn, object customOut)
            {
                _dTEEvents.OnBeginShutdown -= DTEEvents_OnBeginShutdown;
                if (_closeCommandEvents != null)
                {
                    _closeCommandEvents.AfterExecute -= CommandEvents_AfterExecute;
                }
            }

            private void DTEEvents_OnBeginShutdown()
            {
                if (StartProcessSafe(_startInfo, DisplayError) == ProcessStartResult.Ok)
                {
                    //currentProcess.Kill();
                }
            }
        }
    }
}