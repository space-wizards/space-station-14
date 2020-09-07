using Content.Server.GameObjects.Components.Atmos;
using JetBrains.Annotations;
using Robust.Shared.GameObjects.Systems;

namespace Content.Server.GameObjects.EntitySystems
{
    [UsedImplicitly]
    public class GasTankSystem: EntitySystem
    {
        private const float TimePerUpdate = 1f;

        private float _timer;

        public override void Update(float frameTime)
        {
            _timer += frameTime;
            if (_timer < TimePerUpdate) return;
            _timer = 0f;

            foreach (var gasTankComponent in ComponentManager.EntityQuery<GasTankComponent>())
            {
                gasTankComponent.Update();
            }
        }

    }
}
