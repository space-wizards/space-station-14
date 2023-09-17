using Content.Shared.Actions;
using Content.Shared.Actions.Events;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.Damage;

/// <summary>
/// Adds an action ability that will cause all flammable targets in a radius to ignite, also heals the owner
/// of the component when used.
/// </summary>
namespace Content.Server.Abilities.Firestarter
{
    public sealed class FirestarterSystem : EntitySystem
    {
        [Dependency] private readonly DamageableSystem _damageable = default!;
        [Dependency] private readonly EntityLookupSystem _lookup = default!;
        [Dependency] private readonly FlammableSystem _flammable = default!;
        [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
        [Dependency] private readonly SharedAudioSystem _audio = default!;
        [Dependency] private readonly SharedContainerSystem _container = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<FirestarterComponent, ComponentInit>(OnComponentInit);
            SubscribeLocalEvent<FirestarterComponent, FireStarterActionEvent>(OnStartFire);
        }

        /// <summary>
        /// Adds the firestarter action.
        /// </summary>
        private void OnComponentInit(EntityUid uid, FirestarterComponent component, ComponentInit args)
        {
            _actionsSystem.AddAction(uid, ref component.FireStarterActionEntity, component.FireStarterAction, uid);
        }

        /// <summary>
        /// Ignites nearby flammable objects.
        /// </summary>
        private void OnStartFire(EntityUid uid, FirestarterComponent component, FireStarterActionEvent args)
        {

            if (_container.IsEntityOrParentInContainer(uid))
                return;

            var xform = Transform(uid);
            var ignitionRadius = component.IgnitionRadius;
            IgniteNearby(uid, xform.Coordinates, args.Severity, ignitionRadius);
            _damageable.TryChangeDamage(uid, component.HealingOnFire, true, false);
            _audio.PlayPvs(component.IgniteSound, uid);

            args.Handled = true;
        }

        public void IgniteNearby(EntityUid uid, EntityCoordinates coordinates, float severity, float radius)
        {
            foreach (var flammable in _lookup.GetComponentsInRange<FlammableComponent>(coordinates, radius))
            {
                var ent = flammable.Owner;
                var stackAmount = 2 + (int) (severity / 0.15f);
                _flammable.AdjustFireStacks(ent, stackAmount, flammable);
                _flammable.Ignite(ent, uid, flammable);
            }
        }
    }
}
