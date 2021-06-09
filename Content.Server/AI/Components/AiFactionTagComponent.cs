#nullable enable
using Content.Server.AI.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.AI.Components
{
    [RegisterComponent]
    public sealed class AiFactionTagComponent : Component
    {
        public override string Name => "AiFactionTag";

        [DataField("factions")]
        public Faction Factions { get; private set; } = Faction.None;
    }
}
