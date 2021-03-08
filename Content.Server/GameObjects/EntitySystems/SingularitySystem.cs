using Content.Server.GameObjects.Components.Singularity;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Content.Server.GameObjects.EntitySystems
{
    [UsedImplicitly]
    public class SingularitySystem : EntitySystem
    {
        private float _accumulator;

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            _accumulator += frameTime;

            while (_accumulator > 1.0f)
            {
                _accumulator -= 1.0f;

                foreach (var singularity in ComponentManager.EntityQuery<SingularityComponent>())
                {
                    singularity.Update(1);
                }
            }
        }
    }
}
