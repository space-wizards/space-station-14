using Content.Server.GameObjects.Components.Atmos;
using JetBrains.Annotations;
using Robust.Shared.GameObjects.Systems;

namespace Content.Server.GameObjects.EntitySystems
{
    [UsedImplicitly]
    public class BarotraumaSystem : EntitySystem
    {
        private const float TimePerUpdate = 3f;

        private float _timer = 0f;

        /// <inheritdoc />
        public override void Update(float frameTime)
        {
            _timer += frameTime;

            if (_timer < TimePerUpdate) return;

            _timer = 0f;

            foreach (var barotraumaComp in ComponentManager.EntityQuery<BarotraumaComponent>())
            {
                barotraumaComp.Update(frameTime);
            }
        }
    }
}
