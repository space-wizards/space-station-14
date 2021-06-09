using Content.Server.GameObjects.Components.Singularity;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Content.Server.GameObjects.EntitySystems
{
    [UsedImplicitly]
    public class SingularitySystem : EntitySystem
    {
        private float _updateInterval = 1.0f;
        private float _accumulator;

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            _accumulator += frameTime;

            while (_accumulator > _updateInterval)
            {
                _accumulator -= _updateInterval;

                foreach (var singularity in ComponentManager.EntityQuery<ServerSingularityComponent>())
                {
                    singularity.Update(1);
                }
            }

        }
    }
}
