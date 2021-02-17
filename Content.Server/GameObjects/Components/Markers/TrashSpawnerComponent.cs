using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Random;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Markers
{
    [RegisterComponent]
    public class TrashSpawnerComponent : ConditionalSpawnerComponent
    {
        [Dependency] private readonly IRobustRandom _robustRandom = default!;

        public override string Name => "TrashSpawner";

        [ViewVariables(VVAccess.ReadWrite)]
        public List<string> RarePrototypes { get; set; } = new();

        [ViewVariables(VVAccess.ReadWrite)]
        public float RareChance { get; set; } = 0.05f;

        [ViewVariables(VVAccess.ReadWrite)]
        public float Offset { get; set; } = 0.2f;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(this, x => x.RarePrototypes, "rarePrototypes", new List<string>());
            serializer.DataField(this, x => x.RareChance, "rareChance", 0.05f);
            serializer.DataField(this, x => x.Offset, "offset", 0.2f);
        }
        public override void Spawn()
        {
            if (RarePrototypes.Count > 0 && (RareChance == 1.0f || _robustRandom.Prob(RareChance)))
            {
                Owner.EntityManager.SpawnEntity(_robustRandom.Pick(RarePrototypes), Owner.Transform.Coordinates);
                return;
            }

            if (Chance != 1.0f && !_robustRandom.Prob(Chance))
            {
                return;
            }

            if (Prototypes.Count == 0)
            {
                Logger.Warning($"Prototype list in TrashSpawnComponent is empty! Entity: {Owner}");
                return;
            }

            if(!Owner.Deleted)
            {
                var random = IoCManager.Resolve<IRobustRandom>();

                var x_negative = random.Prob(0.5f) ? -1 : 1;
                var y_negative = random.Prob(0.5f) ? -1 : 1;

                var entity = Owner.EntityManager.SpawnEntity(_robustRandom.Pick(Prototypes), Owner.Transform.Coordinates);
                entity.Transform.LocalPosition += new Vector2(random.NextFloat() * Offset * x_negative, random.NextFloat() * Offset * y_negative);
            }

        }

        public override void MapInit()
        {
            Spawn();
            Owner.Delete();
        }
    }
}
