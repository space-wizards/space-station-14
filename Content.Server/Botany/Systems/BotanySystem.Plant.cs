using Content.Server.Botany.Components;
using Content.Server.Chemistry.EntitySystems;
using Content.Server.Popups;
using JetBrains.Annotations;
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

        private float _timer = 0f;

        public override void Initialize()
        {
            base.Initialize();

            InitializeSeeds();
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            _timer += frameTime;
            if (_timer < 3f)
                return;

            _timer -= 3f;

            foreach (var plantHolder in EntityManager.EntityQuery<PlantHolderComponent>())
            {
                plantHolder.Update();
            }
        }
    }
}
