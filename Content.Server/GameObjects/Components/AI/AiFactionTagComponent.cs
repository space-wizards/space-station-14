#nullable enable
using System;
using System.Collections.Generic;
using Content.Server.GameObjects.EntitySystems.AI;
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

            serializer.DataReadWriteFunction(
                "factions",
                new List<Faction>(),
                factions => factions.ForEach(faction => Factions |= faction),
                () =>
                {
                    var writeFactions = new List<Faction>();
                    foreach (Faction fac in Enum.GetValues(typeof(Faction)))
                    {
                        if ((Factions & fac) != 0)
                        {
                            writeFactions.Add(fac);
                        }
                    }

                    return writeFactions;
                });
        }
    }
}
