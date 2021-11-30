using System.Collections.Generic;
using Content.Server.Botany;
using Content.Server.Botany.Components;
using Content.Shared.GameTicking;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;

namespace Content.Server.Plants
{
    [UsedImplicitly]
    public class PlantSystem : EntitySystem
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        private int _nextUid = 0;
        private readonly Dictionary<int, Seed> _seeds = new();

        private float _timer = 0f;

        public IReadOnlyDictionary<int, Seed> Seeds => _seeds;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<RoundRestartCleanupEvent>(Reset);

            PopulateDatabase();
        }

        private void PopulateDatabase()
        {
            _nextUid = 0;

            _seeds.Clear();

            foreach (var seed in _prototypeManager.EnumeratePrototypes<Seed>())
            {
                AddSeedToDatabase(seed);
            }
        }

        public bool AddSeedToDatabase(Seed seed)
        {
            // If it's not -1, it's already in the database. Probably.
            if (seed.Uid != -1)
                return false;

            seed.Uid = GetNextSeedUid();
            _seeds[seed.Uid] = seed;
            return true;
        }

        private int GetNextSeedUid()
        {
            return _nextUid++;
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            _timer += frameTime;
            if (_timer < 3f)
                return;

            _timer = 0f;

            foreach (var plantHolder in EntityManager.EntityQuery<PlantHolderComponent>())
            {
                plantHolder.Update();
            }
        }

        public void Reset(RoundRestartCleanupEvent ev)
        {
            PopulateDatabase();
        }
    }
}
