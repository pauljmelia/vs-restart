// -----------------------------------------------------------------------
// <copyright file="ElevationChecker.cs" company="Equilogic (Pty) Ltd">
//     Copyright © Equilogic (Pty) Ltd. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Equilogic.VisualStudio.VsRestart
{
    using System;
    using System.Runtime.InteropServices;

    internal static class ElevationChecker
    {
        public const int TOKEN_QUERY = 0x00000008;

        public enum TokenInformation
        {
            TokenUser = 1,
            TokenGroups = 2,
            TokenPrivileges = 3,
            TokenOwner = 4,
            TokenPrimaryGroup = 5,
            TokenDefaultDacl = 6,
            TokenSource = 7,
            TokenType = 8,
            TokenImpersonationLevel = 9,
            TokenStatistics = 10,
            TokenRestrictedSids = 11,
            TokenSessionId = 12,
            TokenGroupsAndPrivileges = 13,
            TokenSessionReference = 14,
            TokenSandBoxInert = 15,
            TokenAuditPolicy = 16,
            TokenOrigin = 17,
            TokenElevationType = 18,
            TokenLinkedToken = 19,
            TokenElevation = 20,
            TokenHasRestrictions = 21,
            TokenAccessInformation = 22,
            TokenVirtualizationAllowed = 23,
            TokenVirtualizationEnabled = 24,
            TokenIntegrityLevel = 25,
            TokenUiAccess = 26,
            TokenMandatoryPolicy = 27,
            TokenLogonSid = 28,
            MaxTokenInfoClass = 29
        }

        private enum TokenElevationType
        {
            TokenElevationTypeDefault = 1,
            TokenElevationTypeFull = 2,
            TokenElevationTypeLimited = 3
        }

        public static bool CanCheckElevation => IsVistaOrLater();

        public static bool IsElevated(IntPtr procHandle)
        {
            var isOpenProcToken = NativeMethods.OpenProcessToken(procHandle, TOKEN_QUERY, out var tokenHandle);

            try
            {
                if (isOpenProcToken)
                {
                    TOKEN_ELEVATION te;
                    te.TokenIsElevated = 0;

                    var tokenInformation = Marshal.AllocHGlobal(0);

                    var size = Marshal.SizeOf(Enum.GetUnderlyingType(typeof(TokenInformation)));

                    try
                    {
                        var success = NativeMethods.GetTokenInformation(tokenHandle,
                                                                        TokenInformation.TokenElevationType,
                                                                        ref tokenInformation,
                                                                        size,
                                                                        out var returnLength);
                        if (success && size == returnLength)
                        {
                            return (TokenElevationType) (int) tokenInformation == TokenElevationType.TokenElevationTypeFull;
                        }
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(tokenInformation);
                    }
                }
            }
            finally
            {
                NativeMethods.CloseHandle(tokenHandle);
            }

            return false;
        }

        private static bool IsVistaOrLater()
        {
            return Environment.OSVersion.Version.Major >= 6;
        }

        public struct TOKEN_ELEVATION
        {
            public uint TokenIsElevated;
        }
    }
}