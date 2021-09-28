using Content.Server.Atmos.Components;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.Atmos.EntitySystems
{
    [UsedImplicitly]
    public class GasTankSystem : EntitySystem
    {
        [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;

        private const float TimerDelay = 0.5f;
        private float _timer = 0f;

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            _timer += frameTime;

            if (_timer < TimerDelay) return;
            _timer -= TimerDelay;

            foreach (var gasTank in EntityManager.EntityQuery<GasTankComponent>())
            {
                _atmosphereSystem.React(gasTank.Air, gasTank);
                gasTank.CheckStatus();
                gasTank.UpdateUserInterface();
            }
        }
    }
}
