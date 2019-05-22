// -----------------------------------------------------------------------
// <copyright file="ArgumentTokenCollection.cs" company="Equilogic (Pty) Ltd">
//     Copyright © Equilogic (Pty) Ltd. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Equilogic.VisualStudio.VsRestart.Arguments
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Text;

    internal class ArgumentTokenCollection : IEnumerable<IArgumentToken>
    {
        // ReSharper disable once UnusedMember.Global
        public static readonly ArgumentTokenCollection Empty = new ArgumentTokenCollection();
        private readonly List<IArgumentToken> _arguments;

        public ArgumentTokenCollection()
        {
            _arguments = new List<IArgumentToken>();
        }

        public void Add(IArgumentToken token)
        {
            _arguments.Add(token);
        }

        public void Replace<TArgument>(IArgumentToken target) where TArgument : class, IArgumentToken
        {
            var source = _arguments.OfType<TArgument>().FirstOrDefault();
            if (source != null)
            {
                var index = _arguments.IndexOf(source);
                if (index >= 0)
                {
                    _arguments[index] = target;
                }

                Debug.Assert(_arguments.IndexOf(source) == -1);
            }
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            using (var enumerator = GetEnumerator())
            {
                if (enumerator.MoveNext())
                {
                    sb.Append(enumerator.Current);
                }

                while (enumerator.MoveNext())
                {
                    sb.Append(" ").Append(enumerator.Current);
                }
            }

            return sb.ToString();
        }

        #region  IEnumerable Members

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        #region  IEnumerable<IArgumentToken> Members

        public IEnumerator<IArgumentToken> GetEnumerator()
        {
            return _arguments.GetEnumerator();
        }

        #endregion
    }
}