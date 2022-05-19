using Content.Server.AI.EntitySystems;

namespace Content.Server.AI.Components
{
    [RegisterComponent]
    public sealed class AiFactionTagComponent : Component
    {
        [DataField("factions")]
        public Faction Factions { get; private set; } = Faction.None;
    }
}
