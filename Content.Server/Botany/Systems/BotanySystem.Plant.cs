using System.Collections.Generic;
using Content.Server.Botany.Components;
using Content.Server.Chemistry.EntitySystems;
using Content.Server.Popups;
using Content.Shared.GameTicking;
using Content.Shared.Tag;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Botany.Systems
{
    [UsedImplicitly]
    public sealed partial class BotanySystem : EntitySystem
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly IRobustRandom _robustRandom = default!;
        [Dependency] private readonly SolutionContainerSystem _solutionContainerSystem = default!;
        [Dependency] private readonly TagSystem _tags = default!;

        private int _nextUid = 0;
        private float _timer = 0f;

        public readonly Dictionary<int, SeedPrototype> Seeds = new();

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<RoundRestartCleanupEvent>(Reset);

            InitializeSeeds();

            PopulateDatabase();
        }

        private void PopulateDatabase()
        {
            _nextUid = 0;

            Seeds.Clear();

            foreach (var seed in _prototypeManager.EnumeratePrototypes<SeedPrototype>())
            {
                AddSeedToDatabase(seed);
            }
        }

        public bool AddSeedToDatabase(SeedPrototype seed)
        {
            // If it's not -1, it's already in the database. Probably.
            if (seed.Uid != -1)
                return false;

            seed.Uid = GetNextSeedUid();
            Seeds[seed.Uid] = seed;
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
