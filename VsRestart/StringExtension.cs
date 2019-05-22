// -----------------------------------------------------------------------
// <copyright file="StringExtension.cs" company="Equilogic (Pty) Ltd">
//     Copyright © Equilogic (Pty) Ltd. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Equilogic.VisualStudio.VsRestart
{
    public static class StringExtension
    {
        public static string ReplaceSmart(this string value, string oldValue, string newValue)
        {
            return string.IsNullOrEmpty(oldValue) ? value : value.Replace(oldValue, newValue);
        }
    }
}