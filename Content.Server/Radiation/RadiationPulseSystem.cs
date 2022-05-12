using System.Collections.Generic;
using System.Linq;
using Content.Server.Radiation.Systems;
using Content.Shared.Radiation;
using Content.Shared.Sound;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;

namespace Content.Server.Radiation
{
    [UsedImplicitly]
    public sealed class RadiationPulseSystem : EntitySystem
    {
        [Dependency] private readonly RadiationSystem _radiation = default!;

        private const float RadiationCooldown = 1.0f;
        private float _accumulator;

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            _accumulator += frameTime;

            while (_accumulator > RadiationCooldown)
            {
                _accumulator -= RadiationCooldown;

                // All code here runs effectively every RadiationCooldown seconds, so use that as the "frame time".
                foreach (var comp in EntityManager.EntityQuery<RadiationPulseComponent>())
                {
                    comp.Update(RadiationCooldown);
                    var ent = comp.Owner;

                    if (Deleted(ent)) continue;

                    var cords = Transform(ent).MapPosition;
                    _radiation.IrradiateRange(cords, comp.Range, comp.RadsPerSecond, RadiationCooldown);
                }
            }
        }
    }
}
