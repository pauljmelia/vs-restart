// -----------------------------------------------------------------------
// <copyright file="NativeMethods.cs" company="Equilogic (Pty) Ltd">
//     Copyright © Equilogic (Pty) Ltd. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Equilogic.VisualStudio.VsRestart
{
    using System;
    using System.Runtime.InteropServices;

    internal static class NativeMethods
    {
        [DllImport("Advapi32.dll")]
        public static extern bool OpenProcessToken(IntPtr processHandle, int desiredAccess, out IntPtr tokenHandle);

        [DllImport("Advapi32.dll")]
        public static extern bool GetTokenInformation(IntPtr tokenHandle,
                                                      ElevationChecker.TokenInformation info,
                                                      ref IntPtr tokenInformation,
                                                      int tokenInformationLength,
                                                      out uint returnLength);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CloseHandle(IntPtr hObject);
    }
}