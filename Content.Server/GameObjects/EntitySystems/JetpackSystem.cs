using Content.Server.GameObjects.Components.Jetpack;
using Robust.Shared.GameObjects.Systems;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Server.GameObjects.EntitySystems
{
    public class JetpackSystem : EntitySystem
    {
        private float _timer = 0f;
        private const float Interval = 0.5f;

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            // Same as GasTankSystem
            _timer += frameTime;
            if (_timer < Interval) return;
            _timer = 0f;

            foreach (var jetpackComponent in EntityManager.ComponentManager.EntityQuery<JetpackComponent>())
            {
                jetpackComponent.Update();
            }
        }
    }
}
