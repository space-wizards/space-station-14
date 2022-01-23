using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Random;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Spawners.Components
{
    [RegisterComponent]
    public class RandomSpawnerComponent : ConditionalSpawnerComponent
    {
        [Dependency] private readonly IEntityManager _entMan = default!;
        [Dependency] private readonly IRobustRandom _robustRandom = default!;

        public override string Name => "RandomSpawner";

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("rarePrototypes")]
        public List<string> RarePrototypes { get; set; } = new();

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("rareChance")]
        public float RareChance { get; set; } = 0.05f;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("offset")]
        public float Offset { get; set; } = 0.2f;

        public override void Spawn()
        {
            if (RarePrototypes.Count > 0 && (RareChance == 1.0f || _robustRandom.Prob(RareChance)))
            {
                _entMan.SpawnEntity(_robustRandom.Pick(RarePrototypes), _entMan.GetComponent<TransformComponent>(Owner).Coordinates);
                return;
            }

            if (Chance != 1.0f && !_robustRandom.Prob(Chance))
            {
                return;
            }

            if (Prototypes.Count == 0)
            {
                Logger.Warning($"Prototype list in RandomSpawnerComponent is empty! Entity: {Owner}");
                return;
            }

            if(!_entMan.Deleted(Owner))
            {
                var random = IoCManager.Resolve<IRobustRandom>();

                var x_negative = random.Prob(0.5f) ? -1 : 1;
                var y_negative = random.Prob(0.5f) ? -1 : 1;

                var entity = _entMan.SpawnEntity(_robustRandom.Pick(Prototypes), _entMan.GetComponent<TransformComponent>(Owner).Coordinates);
                _entMan.GetComponent<TransformComponent>(entity).LocalPosition += new Vector2(random.NextFloat() * Offset * x_negative, random.NextFloat() * Offset * y_negative);
            }

        }

        public override void MapInit()
        {
            Spawn();
            _entMan.QueueDeleteEntity(Owner);
        }
    }
}
