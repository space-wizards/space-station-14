using System;
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

    [Flags]
    public enum Faction
    {
        None = 0,
        NanoTrasen = 1 << 0,
        SimpleHostile = 1 << 1,
        SimpleNeutral = 1 << 2,
        Syndicate = 1 << 3,
        Xeno = 1 << 4,
    }
}
