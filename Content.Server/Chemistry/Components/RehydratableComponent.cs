using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Chemistry.Components
{
    /// <summary>
    /// Basically, monkey cubes.
    /// But specifically, this component deletes the entity and spawns in a new entity when the entity is exposed to a given reagent.
    /// </summary>
    [RegisterComponent]
    public class RehydratableComponent : Component
    {
        [ViewVariables]
        [DataField("catalyst")]
        internal string CatalystPrototype = "Water";

        [ViewVariables]
        [DataField("target")]
        internal string? TargetPrototype = default!;

        internal bool Expanding;
    }
}
