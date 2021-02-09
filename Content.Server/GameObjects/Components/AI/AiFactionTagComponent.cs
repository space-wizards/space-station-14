using System;
using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Prototypes.DataClasses.Attributes;
using Robust.Shared.Serialization;

namespace Content.Server.GameObjects.Components.AI
{
    [RegisterComponent]
    [DataClass(typeof(AiFactionTagComponentData))]
    public sealed class AiFactionTagComponent : Component
    {
        public override string Name => "AiFactionTag";

        [DataClassTarget("factions")]
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
