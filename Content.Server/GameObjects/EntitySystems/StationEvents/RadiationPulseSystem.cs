using System.Collections.Generic;
using Content.Server.GameObjects.Components.StationEvents;
using Content.Shared.Damage;
using Content.Shared.GameObjects.Components.Body;
using Content.Shared.GameObjects.Components.Damage;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Server.GameObjects.EntitySystems.StationEvents
{
    [UsedImplicitly]
    public sealed class RadiationPulseSystem : EntitySystem
    {
        // Rather than stuffing around with collidables and checking entities on initialize etc. we'll just tick over
        // for each entity in range. Seemed easier than checking entities on spawn, then checking collidables, etc.
        // Especially considering each pulse is a big chonker, + no circle hitboxes yet.

        private TypeEntityQuery _speciesQuery;

        /// <summary>
        ///     Damage works with ints so we'll just accumulate damage and once we hit this threshold we'll apply it.
        /// </summary>
        /// This also server to stop spamming the damagethreshold with 1 damage continuously.
        private const int DamageThreshold = 10;

        private Dictionary<IEntity, float> _accumulatedDamage = new Dictionary<IEntity, float>();

        public override void Initialize()
        {
            base.Initialize();
            _speciesQuery = new TypeEntityQuery(typeof(ISharedBodyManagerComponent));
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            var anyPulses = false;

            foreach (var comp in ComponentManager.EntityQuery<RadiationPulseComponent>())
            {
                anyPulses = true;

                foreach (var species in EntityManager.GetEntities(_speciesQuery))
                {
                    // Work out if we're in range and accumulate more damage
                    // If we've hit the DamageThreshold we'll also apply that damage to the mob
                    // If we're really lagging server can apply multiples of the DamageThreshold at once
                    if (species.Transform.MapID != comp.Owner.Transform.MapID) continue;

                    if ((species.Transform.WorldPosition - comp.Owner.Transform.WorldPosition).Length > comp.Range)
                    {
                        continue;
                    }

                    var totalDamage = frameTime * comp.DPS;

                    if (!_accumulatedDamage.TryGetValue(species, out var accumulatedSpecies))
                    {
                        _accumulatedDamage[species] = 0.0f;
                    }

                    totalDamage += accumulatedSpecies;
                    _accumulatedDamage[species] = totalDamage;

                    if (totalDamage < DamageThreshold) continue;
                    if (!species.TryGetComponent(out DamageableComponent damageableComponent)) continue;

                    var damageMultiple = (int) (totalDamage / DamageThreshold);
                    _accumulatedDamage[species] = totalDamage % DamageThreshold;

                    damageableComponent.ChangeDamage(DamageType.Heat, damageMultiple * DamageThreshold, false, comp.Owner);
                }
            }

            if (anyPulses)
            {
                return;
            }

            // probably don't need to worry about clearing this at roundreset unless you have a radiation pulse at roundstart
            // (which is currently not possible)
            _accumulatedDamage.Clear();
        }
    }
}
