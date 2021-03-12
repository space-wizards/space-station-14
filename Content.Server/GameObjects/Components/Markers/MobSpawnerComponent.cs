using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Random;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;
using System.Collections.Generic;

namespace Content.Server.GameObjects.Components.Markers
{
    [RegisterComponent]
    public class MobSpawnerComponent : Component, ISerializationHooks
    {
        [Dependency] private readonly IRobustRandom _robustRandom = default!;

        public override string Name => "MobSpawner";

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("prototypes")]
        public List<string> Prototypes;

        public override void Initialize()
        {
            base.Initialize();
            var entity = _robustRandom.Pick(Prototypes);
            Owner.EntityManager.SpawnEntity(entity, Owner.Transform.Coordinates);
        }
    }
}
