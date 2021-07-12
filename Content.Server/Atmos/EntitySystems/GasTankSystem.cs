using Content.Server.Atmos.Components;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Content.Server.Atmos.EntitySystems
{
    [UsedImplicitly]
    public class GasTankSystem : EntitySystem
    {
        private const float TimerDelay = 0.5f;
        private float _timer = 0f;

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            _timer += frameTime;

            if (_timer < TimerDelay) return;
            _timer -= TimerDelay;

            var atmosphereSystem = Get<AtmosphereSystem>();

            foreach (var gasTank in EntityManager.ComponentManager.EntityQuery<GasTankComponent>())
            {
                atmosphereSystem.React(gasTank.Air, gasTank);
                gasTank.CheckStatus();
                gasTank.UpdateUserInterface();
            }
        }
    }
}
