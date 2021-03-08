#nullable enable
using Content.Server.GameObjects.EntitySystems.AI;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.GameObjects.Components.AI
{
    [RegisterComponent]
    public sealed class AiFactionTagComponent : Component
    {
        public override string Name => "AiFactionTag";

        [DataField("factions")]
        public Faction Factions { get; private set; } = Faction.None;
    }
}
