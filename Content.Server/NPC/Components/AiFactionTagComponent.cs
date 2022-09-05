using Content.Server.NPC.Systems;

namespace Content.Server.NPC.Components
{
    [RegisterComponent]
    public sealed class AiFactionTagComponent : Component
    {
        [DataField("factions")]
        public Faction Factions { get; set; } = Faction.None;
    }
}
