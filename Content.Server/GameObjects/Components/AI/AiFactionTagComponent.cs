using System;
using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Server.GameObjects.Components.AI
{
    [RegisterComponent]
    public sealed class AiFactionTagComponent : Component
    {
        public override string Name => "AiFactionTag";

        public Faction Factions { get; private set; } = Faction.None;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            var factions = serializer.ReadDataField("factions", new List<Faction>());

            foreach (var faction in factions)
            {
                Factions |= faction;
            }
        }
    }

    [Flags]
    public enum Faction
    {
        None = 0,
        NanoTransen = 1 << 0,
        SimpleHostile = 1 << 1,
        SimpleNeutral = 1 << 2,
        Syndicate = 1 << 3,
        Xeno = 1 << 4,
    }
}