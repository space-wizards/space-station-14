using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Content.Server.NodeContainer.NodeGroups;
using Content.Server.NodeContainer.Nodes;
using Content.Shared.Examine;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Server.Salvage
{
    /// <summary>
    ///     Mmmm, salvage
    ///     To be clear, this component is SUPPOSED to later handle magnet attraction.
    /// </summary>
    [RegisterComponent]
    [ComponentProtoName("Salvage")]
    public class SalvageComponent : Component
    {
        /// <summary>
        ///     The salvage will be deleted when no actor remains parented to it, and will be thrown away from the station.
        ///     Set this when salvage is lost by the magnet.
        /// </summary>
        [ViewVariables] public bool Killswitch = false;

        /// <summary>
        ///     Time killswitch has been active for
        /// </summary>
        [ViewVariables] public float KillswitchTime = 0.0f;
    }
}
