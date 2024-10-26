using System.Linq;
using Content.Server.Falling;
using Content.Shared.Damage.Systems;
using Content.Shared.Damage;
using Content.Shared.Stunnable;
using Content.Shared.Damage.Components;
using Robust.Shared.Map;
using Robust.Shared.Timing;
using Content.Server.Popups;
using Content.Shared.Popups;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.GameObjects;

namespace Content.Server.Falling
{
    public sealed class FallSystem : EntitySystem
    {
        [Dependency] private readonly SharedStunSystem _stun = default!;
        [Dependency] private readonly DamageableSystem _damageable = default!;
        [Dependency] private readonly IGameTiming _timing = default!;
        [Dependency] private readonly PopupSystem _popup = default!;
        [Dependency] private readonly IRobustRandom _random = default!; // Dependency for random number generation
        [Dependency] private readonly EntityLookupSystem _lookup = default!; // Add the missing lookup dependency

        private const int MaxRandomTeleportAttempts = 20; // Number of attempts to find a valid teleport location

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<FallSystemComponent, EntParentChangedMessage>(OnEntParentChanged);
        }

        private void OnEntParentChanged(EntityUid owner, FallSystemComponent component, EntParentChangedMessage args)
        {
            if (args.OldParent == null || args.Transform.GridUid != null || TerminatingOrDeleted(owner))
                return;

            // Try to find a FallingDestinationComponent; if none found, teleport randomly
            var destination = EntityManager.EntityQuery<FallingDestinationComponent>().FirstOrDefault();
            if (destination != null)
            {
                // Teleport to the first destination's coordinates
                Transform(owner).Coordinates = Transform(destination.Owner).Coordinates;
            }
            else
            {
                // If no destination is found, teleport randomly within a defined radius
                TeleportRandomly(owner, component);
            }

            // Apply stun and knockdown effects
            var stunTime = TimeSpan.FromSeconds(3);
            _stun.TryKnockdown(owner, stunTime, refresh: true);
            _stun.TryStun(owner, stunTime, refresh: true);

            // Apply 80 blunt damage to the owner
            var damage = new DamageSpecifier
            {
                DamageDict = { ["Blunt"] = 80f } // Using float for damage
            };
            _damageable.TryChangeDamage(owner, damage, origin: owner);
            _popup.PopupEntity(Loc.GetString("fell-to-seafloor"), owner, PopupType.LargeCaution);
        }

        private void TeleportRandomly(EntityUid owner, FallSystemComponent component)
        {
            var coords = Transform(owner).Coordinates;
            var newCoords = coords; // Start with the current coordinates

            for (var i = 0; i < MaxRandomTeleportAttempts; i++)
            {
                // Generate a random offset based on a defined radius
                var offset = _random.NextVector2(component.MaxRandomRadius); // Assume component has MaxRandomRadius
                newCoords = coords.Offset(offset);

                // Check if the new coordinates are free of static entities
                if (!_lookup.GetEntitiesIntersecting(newCoords.ToMap(EntityManager), LookupFlags.Static).Any())
                {
                    break; // Found a valid location
                }
            }

            // Set the new coordinates to teleport the entity
            Transform(owner).Coordinates = newCoords;
        }
    }
}
