using System;
using JetBrains.Annotations;
using Robust.Shared.Console;

namespace Content.Shared.Administration
{
    /// <summary>
    ///     Specifies that a command can be executed by any player.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    [BaseTypeRequired(typeof(IConsoleCommand))]
    [MeansImplicitUse]
    public sealed class AnyCommandAttribute : Attribute
    {

    }
}
