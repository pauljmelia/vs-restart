// -----------------------------------------------------------------------
// <copyright file="GuidList.cs" company="Equilogic (Pty) Ltd">
//     Copyright © Equilogic (Pty) Ltd. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Equilogic.VisualStudio.VsRestart
{
    using System;

    internal static class GuidList
    {
        public const string RestartElevatedCommandGroupId = "15dc28d5-04f4-4698-90e0-e3e16bc6894f";

        public const string TopLevelMenuGroupId = "D2FB6644-0147-4FDB-8F35-22B5F0AA8594";
        public const string VsRestartPackageId = "bf28bdb5-4844-4538-adc3-9416020ced24";

        public static readonly Guid RestartElevatedGroupGuid = new Guid(RestartElevatedCommandGroupId);
        public static readonly Guid TopLevelMenuGuid = new Guid(TopLevelMenuGroupId);
    }
}