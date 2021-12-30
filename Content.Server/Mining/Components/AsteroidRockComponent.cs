using System.Collections.Generic;
using System.Threading.Tasks;
using Content.Server.Storage;
using Content.Server.Weapon.Melee.Components;
using Content.Shared.Damage;
using Content.Shared.Interaction;
using Content.Shared.Mining;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.IoC;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;
using Robust.Shared.ViewVariables;

namespace Content.Server.Mining.Components
{
    [RegisterComponent]
    public class AsteroidRockComponent : Component
    {
        [Dependency] private readonly IEntityManager _entMan = default!;
        [Dependency] private readonly IRobustRandom _random = default!;

        public override string Name => "AsteroidRock";
        private static readonly string[] SpriteStates = {"0", "1", "2", "3", "4"};

        [DataField("oreChance")]
        public float OreChance;

        [DataField("oreTable")]
        public List<EntitySpawnEntry> OreTable = default!;

        protected override void Initialize()
        {
            base.Initialize();
            if (_entMan.TryGetComponent(Owner, out AppearanceComponent? appearance))
            {
                appearance.SetData(AsteroidRockVisuals.State, _random.Pick(SpriteStates));
            }
        }

    }
}
