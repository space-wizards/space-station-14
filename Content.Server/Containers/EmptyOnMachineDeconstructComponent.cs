using System;
using System.Collections.Generic;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Containers
{
    /// <summary>
    /// Empties a list of containers when the machine is deconstructed via MachineDeconstructedEvent.
    /// </summary>
    [RegisterComponent]
    public class EmptyOnMachineDeconstructComponent : Component
    {
        public override string Name => "EmptyOnMachineDeconstruct";

        [ViewVariables]
        [DataField("containers")]
        public HashSet<string> Containers { get; set; } = new();
    }
}
