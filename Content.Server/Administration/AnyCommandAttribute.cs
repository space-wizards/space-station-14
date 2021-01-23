using System;
using JetBrains.Annotations;
using Robust.Server.Console;

namespace Content.Server.Administration
{
    /// <summary>
    ///     Specifies that a command can be executed by any player.
    /// </summary>
    /// <seealso cref="AdminCommandAttribute"/>
    [AttributeUsage(AttributeTargets.Class)]
    [BaseTypeRequired(typeof(IServerCommand))]
    [MeansImplicitUse]
    public sealed class AnyCommandAttribute : Attribute
    {

    }
}
