using System.Collections.Generic;
using System.Linq;
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
        [Dependency] private readonly IEntityManager _entMan = default!;
        [Dependency] private readonly IEntityLookup _lookup = default!;

        private const float RadiationCooldown = 0.5f;
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

                    foreach (var entity in _lookup.GetEntitiesInRange(_entMan.GetComponent<TransformComponent>(ent).Coordinates, comp.Range))
                    {
                        // For now at least still need this because it uses a list internally then returns and this may be deleted before we get to it.
                        if ((!_entMan.EntityExists(entity) ? EntityLifeStage.Deleted : _entMan.GetComponent<MetaDataComponent>(entity).EntityLifeStage) >= EntityLifeStage.Deleted) continue;

                        // Note: Radiation is liable for a refactor (stinky Sloth coding a basic version when he did StationEvents)
                        // so this ToArray doesn't really matter.
                        foreach (var radiation in _entMan.GetComponents<IRadiationAct>(entity).ToArray())
                        {
                            radiation.RadiationAct(RadiationCooldown, comp);
                        }
                    }
                }
            }
        }
    }
}
