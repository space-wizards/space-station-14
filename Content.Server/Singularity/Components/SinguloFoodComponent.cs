using Content.Shared.GameObjects.Components;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.ViewVariables;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.GameObjects.Components.Singularity
{
    /// <summary>
    /// Overrides exactly how much energy this object gives to a singularity.
    /// </summary>
    [RegisterComponent]
    public class SinguloFoodComponent : Component
    {
        public override string Name => "SinguloFood";
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("energy")]
        public int Energy { get; set; } = 1;
    }
}
