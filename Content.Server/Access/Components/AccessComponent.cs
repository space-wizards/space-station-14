using System.Collections.Generic;
using Content.Server.Access.Systems;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Access.Components
{
    /// <summary>
    ///     Simple mutable access provider found on ID cards and such.
    /// </summary>
    [RegisterComponent]
    [Friend(typeof(AccessSystem))]
    public class AccessComponent : Component
    {
        public override string Name => "Access";

        [DataField("tags")]
        [ViewVariables]
        public HashSet<string> Tags = new();
    }
}
