using System;
using Content.Server.GameObjects.Components.Atmos;
using JetBrains.Annotations;
using Robust.Shared.GameObjects.EntitySystemMessages;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;

namespace Content.Server.GameObjects.EntitySystems
{
    [UsedImplicitly]
    public class GasTankSystem : EntitySystem
    {
        private float _timer = 0f;
        private const float Interval = 0.5f;

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            _timer += frameTime;

            if (_timer < Interval) return;
            _timer = 0f;

            foreach (var gasTank in EntityManager.ComponentManager.EntityQuery<GasTankComponent>())
            {
                gasTank.Update();
            }
        }
    }
}
