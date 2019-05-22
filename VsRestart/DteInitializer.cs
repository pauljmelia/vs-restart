// -----------------------------------------------------------------------
// <copyright file="DteInitializer.cs" company="Equilogic (Pty) Ltd">
//     Copyright © Equilogic (Pty) Ltd. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Equilogic.VisualStudio.VsRestart
{
    using System;
    using System.Threading.Tasks;

    using Microsoft.VisualStudio;
    using Microsoft.VisualStudio.Shell.Interop;

    internal class DteInitializer : IVsShellPropertyEvents
    {
        private readonly Func<Task> _callback;
        private readonly IVsShell _shellService;
        private uint _cookie;

        internal DteInitializer(IVsShell shellService, Func<Task> callback)
        {
            _shellService = shellService;
            _callback = callback;

            // Set an event handler to detect when the IDE is fully initialized
            var hr = _shellService.AdviseShellPropertyChanges(this, out _cookie);

            ErrorHandler.ThrowOnFailure(hr);
        }

        #region  IVsShellPropertyEvents Members

        int IVsShellPropertyEvents.OnShellPropertyChange(int propId, object var)
        {
            if (propId != (int) __VSSPROPID.VSSPROPID_Zombie)
            {
                return VSConstants.S_OK;
            }

            var isZombie = (bool) var;

            if (isZombie)
            {
                return VSConstants.S_OK;
            }

            // Release the event handler to detect when the IDE is fully initialized
            var hr = _shellService.UnadviseShellPropertyChanges(_cookie);

            ErrorHandler.ThrowOnFailure(hr);

            _cookie = 0;

            Task.Run(_callback);

            return VSConstants.S_OK;
        }

        #endregion
    }
}