using Content.Server.PowerCell;
using Content.Shared.Interaction.Events;
using Content.Shared.Pinpointer;
using Content.Shared.PowerCell;
using Robust.Server.GameObjects;
using Robust.Shared.Timing;

namespace Content.Server.Pinpointer;

/// <summary>
/// This handles logic and interaction relating to <see cref="ProximityBeeperComponent"/>
/// </summary>
public sealed class ProximityBeeperSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly PowerCellSystem _powerCell = default!;
    [Dependency] private readonly TransformSystem _transform = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ProximityBeeperComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<ProximityBeeperComponent, EntityUnpausedEvent>(OnUnpaused);
        SubscribeLocalEvent<ProximityBeeperComponent, PowerCellSlotEmptyEvent>(OnPowerCellSlotEmpty);
    }
    private void OnUseInHand(EntityUid uid, ProximityBeeperComponent component, UseInHandEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = TryToggle(uid, component, args.User);
    }

    private void OnUnpaused(EntityUid uid, ProximityBeeperComponent component, ref EntityUnpausedEvent args)
    {
        component.NextBeepTime += args.PausedTime;
    }

    private void OnPowerCellSlotEmpty(EntityUid uid, ProximityBeeperComponent component, ref PowerCellSlotEmptyEvent args)
    {
        if (component.Enabled)
            TryDisable(uid, component);
    }

    /// <summary>
    /// Beeps the proximitybeeper as well as sets the time for the next beep
    /// based on proximity to entities with the target component.
    /// </summary>
    public void UpdateBeep(EntityUid uid, ProximityBeeperComponent? component = null, bool playBeep = true)
    {
        if (!Resolve(uid, ref component))
            return;

        if (!component.Enabled)
        {
            component.NextBeepTime += component.MaxBeepInterval;
            return;
        }

        var xformQuery = GetEntityQuery<TransformComponent>();
        var xform = xformQuery.GetComponent(uid);
        var compType = EntityManager.ComponentFactory.GetRegistration(component.Component).Type;
        float? closestDistance = null;
        foreach (var ent in _entityLookup.GetEntitiesInRange(compType, xform.MapPosition, component.MaximumDistance))
        {
            var dist = (_transform.GetWorldPosition(xform, xformQuery) - _transform.GetWorldPosition(ent, xformQuery)).Length();
            if (dist >= (closestDistance ?? float.MaxValue))
                continue;
            closestDistance = dist;
        }

        if (closestDistance is not { } distance)
            return;

        if (playBeep)
            _audio.PlayPvs(component.BeepSound, uid);

        var scalingFactor = distance / component.MaximumDistance;
        var interval = (component.MaxBeepInterval - component.MinBeepInterval) * scalingFactor + component.MinBeepInterval;

        component.NextBeepTime += interval;
        if (component.NextBeepTime < _timing.CurTime) // Prevents spending time out of range accumulating a deficit which causes a series of very rapid beeps when comeing into range.
            component.NextBeepTime = _timing.CurTime + interval;
    }

    /// <summary>
    /// Enables the proximity beeper
    /// </summary>
    public bool TryEnable(EntityUid uid, ProximityBeeperComponent? component = null, EntityUid? user = null)
    {
        if (!Resolve(uid, ref component))
            return false;

        TryComp<PowerCellDrawComponent>(uid, out var draw);

        if (!_powerCell.HasActivatableCharge(uid, battery: draw, user: user))
            return false;

        component.Enabled = true;
        _appearance.SetData(uid, ProximityBeeperVisuals.Enabled, true);
        component.NextBeepTime = _timing.CurTime;
        UpdateBeep(uid, component, false);
        if (draw != null)
            _powerCell.SetPowerCellDrawEnabled(uid, true, draw);
        return true;
    }

    /// <summary>
    /// Disables the proximity beeper
    /// </summary>
    public bool TryDisable(EntityUid uid, ProximityBeeperComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return false;

        if (!component.Enabled)
            return false;

        component.Enabled = false;
        _appearance.SetData(uid, ProximityBeeperVisuals.Enabled, false);
        _powerCell.SetPowerCellDrawEnabled(uid, false);
        UpdateBeep(uid, component);
        return true;
    }

    /// <summary>
    /// toggles the proximity beeper
    /// </summary>
    public bool TryToggle(EntityUid uid, ProximityBeeperComponent? component = null, EntityUid? user = null)
    {
        if (!Resolve(uid, ref component))
            return false;

        return component.Enabled
            ? TryDisable(uid, component)
            : TryEnable(uid, component, user);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ProximityBeeperComponent>();
        while (query.MoveNext(out var uid, out var beeper))
        {
            if (!beeper.Enabled)
                continue;

            if (_timing.CurTime < beeper.NextBeepTime)
                continue;
            UpdateBeep(uid, beeper);
        }
    }
}
