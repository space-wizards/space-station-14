using System;
using System.Collections.Generic;

namespace Content.Server.Access.Components
{
    /// <summary>
    ///     Contains access levels that can be checked to see if somebody has access with an <see cref="AccessReader"/>.
    /// </summary>
    public interface IAccess
    {
        /// <summary>
        ///     The set of access tags this thing has.
        /// </summary>
        /// <remarks>
        ///     This set may be read-only. Check <see cref="IsReadOnly"/> if you want to mutate it.
        /// </remarks>
        ISet<string> Tags { get; }

        /// <summary>
        ///     Whether the <see cref="Tags"/> list is read-only.
        /// </summary>
        bool IsReadOnly { get; }

        /// <summary>
        ///     Replaces the set of access tags we have with the provided set.
        /// </summary>
        /// <param name="newTags">The new access tags</param>
        /// <exception cref="NotSupportedException">If this access tag list is read-only.</exception>
        void SetTags(IEnumerable<string> newTags);
    }
}
