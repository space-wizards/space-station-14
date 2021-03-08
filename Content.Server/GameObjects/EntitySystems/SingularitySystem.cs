using Content.Server.GameObjects.Components.Singularity;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Content.Server.GameObjects.EntitySystems
{
    [UsedImplicitly]
    public class SingularitySystem : EntitySystem
    {
        private readonly float _updateInterval = 1.0f;
        private readonly float _pullInterval = 0.0f;
        private float _updateTime, _pullTime;
        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            _updateTime += frameTime;
            _pullTime += frameTime;

            if (_updateTime >= _updateInterval || _pullTime >= _pullInterval)
                return;

            var singulos = ComponentManager.EntityQuery<ServerSingularityComponent>(true);

            if (_updateTime >= _updateInterval)
            {
                _updateTime -= _updateInterval;
                foreach (var singulo in singulos)
                    singulo.Update();
            }

            if (_pullTime >= _pullInterval)
            {
                _pullTime -= _pullInterval;
                foreach (var singulo in singulos)
                    singulo.PullUpdate();
            }

        }
    }
}
