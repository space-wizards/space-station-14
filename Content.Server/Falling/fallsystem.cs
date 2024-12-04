using System.Linq;
using Content.Shared.Ghost;
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
using Content.Shared.Gravity;

namespace Content.Server.Falling
{
    public sealed class FallSystem : EntitySystem
    {
        [Dependency] private readonly SharedStunSystem _stun = default!;
        [Dependency] private readonly DamageableSystem _damageable = default!;
        [Dependency] private readonly IGameTiming _timing = default!;
        [Dependency] private readonly PopupSystem _popup = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly EntityLookupSystem _lookup = default!;

        private const int MaxRandomTeleportAttempts = 20; // The # of times it's going to try to find a valid spot to randomly teleport an object

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<FallSystemComponent, EntParentChangedMessage>(OnEntParentChanged);
        }

        private void OnEntParentChanged(EntityUid owner, FallSystemComponent component, EntParentChangedMessage args) // called when the entity changes parents
        {
            if (args.OldParent == null || args.Transform.GridUid != null || TerminatingOrDeleted(owner)) // If you came from space or are switching to another valid grid, nothing happens.
                return;

            if (HasComp<GhostComponent>(owner))
            return;

            if (HasComp<TriesteComponent>(args.OldParent))
            {

            // Try to find an object with the FallingDestinationComponent
            var destination = EntityManager.EntityQuery<FallingDestinationComponent>().FirstOrDefault();
            if (destination != null)
            {
                // Teleport to the first destination's coordinates
                Transform(owner).Coordinates = Transform(destination.Owner).Coordinates;
            }
            else
            {
                // If there's no destination, something broke
                Log.Error($"No valid falling sites available!");
                return;
            }

            // Stuns the fall-ee for five seconds
            var stunTime = TimeSpan.FromSeconds(5);
            _stun.TryKnockdown(owner, stunTime, refresh: true);
            _stun.TryStun(owner, stunTime, refresh: true);

            // Defines the damage being dealt
            var damage = new DamageSpecifier
            {
                DamageDict = { ["Blunt"] = 80f }
            };
            _damageable.TryChangeDamage(owner, damage, origin: owner);
            // Causes a popup
            _popup.PopupEntity(Loc.GetString("fell-to-seafloor"), owner, PopupType.LargeCaution);
            // Randomly teleports you in a radius around the landing zone
            TeleportRandomly(owner, component);

            }
        }

        private void TeleportRandomly(EntityUid owner, FallSystemComponent component)
        {
            var coords = Transform(owner).Coordinates;
            var newCoords = coords; // Start with the current coordinates

            for (var i = 0; i < MaxRandomTeleportAttempts; i++)
            {
                // Generate a random offset based on a defined radius
                var offset = _random.NextVector2(component.MaxRandomRadius);
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
