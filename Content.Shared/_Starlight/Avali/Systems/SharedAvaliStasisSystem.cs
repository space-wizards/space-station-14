using Content.Shared.Actions;
using Content.Shared.Administration;
using Content.Shared.Starlight.Avali.Components;
using Content.Shared.Popups;
using Robust.Shared.Localization;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Spawners;

namespace Content.Shared.Starlight.Avali.Systems;

/// <summary>
/// Allows mobs to enter nanite induced stasis <see cref="AvaliStasisComponent"/>.
/// </summary>
public abstract class SharedAvaliStasisSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly SharedTransformSystem _xformSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AvaliStasisComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<AvaliStasisComponent, ComponentShutdown>(OnCompRemove);
        SubscribeLocalEvent<AvaliStasisComponent, AvaliEnterStasisActionEvent>(OnEnterStasisStart);
        SubscribeLocalEvent<AvaliStasisComponent, AvaliExitStasisActionEvent>(OnExitStasisStart);
    }

    /// <summary>
    /// Giveths the action to preform stasis on the entity
    /// </summary>
    private void OnMapInit(EntityUid uid, AvaliStasisComponent comp, MapInitEvent args)
    {
        _actionsSystem.AddAction(uid, ref comp.EnterStasisActionEntity, comp.EnterStasisAction);
    }

    /// <summary>
    /// Takeths away the action to preform stasis from the entity.
    /// </summary>
    private void OnCompRemove(EntityUid uid, AvaliStasisComponent comp, ComponentShutdown args)
    {
        _actionsSystem.RemoveAction(uid, comp.EnterStasisActionEntity);
        _actionsSystem.RemoveAction(uid, comp.ExitStasisActionEntity);
    }

    private void OnEnterStasisStart(EntityUid uid, AvaliStasisComponent comp,
        AvaliEnterStasisActionEvent args)
    {
        if (comp.IsInStasis)
        {
            return;
        }

        comp.IsInStasis = true;
        EnsureComp<AvaliStasisFrozenComponent>(uid);

        // Add stasis effect
        EnsureComp<TransformComponent>(uid, out var xform);
        var effectEnt = SpawnAttachedTo(comp.StasisEnterEffect, xform.Coordinates);
        _xformSystem.SetParent(effectEnt, uid);
        EnsureComp<TimedDespawnComponent>(effectEnt, out var despawnEffectEntComp);
        despawnEffectEntComp.Lifetime = comp.StasisEnterEffectLifetime;
        _audioSystem.PlayPvs(comp.StasisEnterSound, effectEnt);

        _popupSystem.PopupEntity(Loc.GetString("avali-stasis-entering"), uid, PopupType.Medium);

        _actionsSystem.RemoveAction(uid, comp.EnterStasisActionEntity);
        _actionsSystem.AddAction(uid, ref comp.ExitStasisActionEntity, comp.ExitStasisAction);
    }

    private void OnExitStasisStart(EntityUid uid, AvaliStasisComponent comp, AvaliExitStasisActionEvent args)
    {
        if (!comp.IsInStasis)
        {
            return;
        }

        _popupSystem.PopupEntity(Loc.GetString("avali-stasis-exiting"), uid, PopupType.Medium);

        _actionsSystem.RemoveAction(uid, comp.ExitStasisActionEntity);
        _actionsSystem.AddAction(uid, ref comp.EnterStasisActionEntity, comp.EnterStasisAction);
        _actionsSystem.SetCooldown(comp.EnterStasisActionEntity, TimeSpan.FromSeconds(comp.StasisCooldown));

        comp.IsInStasis = false;
        RemComp<AvaliStasisFrozenComponent>(uid);
    }
}

/// <summary>
/// Should be relayed upon using the action.
/// </summary>
public sealed partial class AvaliEnterStasisActionEvent : InstantActionEvent
{
}

/// <summary>
/// Should be relayed upon using the action.
/// </summary>
public sealed partial class AvaliExitStasisActionEvent : InstantActionEvent
{
}