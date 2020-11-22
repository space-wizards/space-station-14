using System;
using JetBrains.Annotations;
using Robust.Server.Interfaces.Console;

namespace Content.Server.Administration
{
    /// <summary>
    ///     Specifies that a command can be executed by any player.
    /// </summary>
    /// <seealso cref="AdminCommandAttribute"/>
    [AttributeUsage(AttributeTargets.Class)]
    [BaseTypeRequired(typeof(IClientCommand))]
    [MeansImplicitUse]
    public sealed class AnyCommandAttribute : Attribute
    {

    }
}
