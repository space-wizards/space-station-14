using Content.Shared.Actions.Events;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Robust.Shared.Audio.Systems;
using Content.Shared.Abilities.Firestarter;

/// <summary>
/// Adds an action ability that will cause all flammable targets in a radius to ignite, also heals the owner
/// of the component when used.
/// </summary>
namespace Content.Server.Abilities.Firestarter;

public sealed class FirestarterSystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly FlammableSystem _flammable = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<FirestarterComponent, FireStarterActionEvent>(OnStartFire);
    }

    /// <summary>
    /// Checks Radius for igniting nearby flammable objects .
    /// </summary>
    private void OnStartFire(EntityUid uid, FirestarterComponent component, FireStarterActionEvent args)
    {

        if (_container.IsEntityOrParentInContainer(uid))
            return;

        var xform = Transform(uid);
        var ignitionRadius = component.IgnitionRadius;
        IgniteNearby(uid, xform.Coordinates, args.Severity, ignitionRadius);
        _audio.PlayPvs(component.IgniteSound, uid);

        args.Handled = true;
    }

    /// <summary>
    /// Ignites flammable objects within range.
    /// </summary>
    public void IgniteNearby(EntityUid uid, EntityCoordinates coordinates, float severity, float radius)
    {
        var flammables = new HashSet<Entity<FlammableComponent>>();
        _lookup.GetEntitiesInRange(coordinates, radius, flammables);

        foreach (var flammable in flammables)
        {
            var ent = flammable.Owner;
            var stackAmount = 2 + (int) (severity / 0.15f);
            _flammable.AdjustFireStacks(ent, stackAmount, flammable);
            _flammable.Ignite(ent, uid, flammable);
        }
    }
}
