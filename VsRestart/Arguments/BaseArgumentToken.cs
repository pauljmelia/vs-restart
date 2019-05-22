// -----------------------------------------------------------------------
// <copyright file="BaseArgumentToken.cs" company="Equilogic (Pty) Ltd">
//     Copyright © Equilogic (Pty) Ltd. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Equilogic.VisualStudio.VsRestart.Arguments
{
    internal abstract class BaseArgumentToken : IArgumentToken
    {
        protected BaseArgumentToken(string argument)
        {
            Argument = argument;
        }

        public string Argument { get; }

        public override string ToString()
        {
            return Argument;
        }
    }
}