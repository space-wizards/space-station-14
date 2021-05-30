using System;
using JetBrains.Annotations;
using Robust.Shared.Console;

namespace Content.Server.Administration
{
    /// <summary>
    ///     Specifies that a command can be executed by any player.
    /// </summary>
    /// <seealso cref="AdminCommandAttribute"/>
    [AttributeUsage(AttributeTargets.Class)]
    [BaseTypeRequired(typeof(IConsoleCommand))]
    [MeansImplicitUse]
    public sealed class AnyCommandAttribute : Attribute
    {

    }
}
