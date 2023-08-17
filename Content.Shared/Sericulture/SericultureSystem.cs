using Content.Shared.Actions;
using Content.Shared.Actions.ActionTypes;
using Content.Shared.DoAfter;
using Content.Shared.Nutrition.EntitySystems;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Content.Shared.Popups;
using Robust.Shared.Network;

namespace Content.Shared.Sericulture;

/// <summary>
/// Allows mobs to produce materials with <see cref="SericultureComponent"/>.
/// </summary>
public abstract partial class SharedSericultureSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly IPrototypeManager _protoManager = default!;
    [Dependency] private readonly HungerSystem _hungerSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly INetManager _netManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SericultureComponent, ComponentInit>(OnCompInit);
        SubscribeLocalEvent<SericultureComponent, ComponentShutdown>(OnCompRemove);
        SubscribeLocalEvent<SericultureComponent, SericultureActionEvent>(OnSericultureStart);
    }

    /// <summary>
    /// Giveths the action to preform sericulture on the entity
    /// </summary>
    private void OnCompInit(EntityUid uid, SericultureComponent comp, ComponentInit args)
    {
        if (!_protoManager.TryIndex<InstantActionPrototype>(comp.ActionProto, out var actionProto))
            return;

        _actionsSystem.AddAction(uid, new InstantAction(actionProto), uid);
    }

    /// <summary>
    /// Takeths away the action to preform sericulture from the entity.
    /// </summary>
    private void OnCompRemove(EntityUid uid, SericultureComponent comp, ComponentShutdown args)
    {
        if (!_protoManager.TryIndex<InstantActionPrototype>(comp.ActionProto, out var actionProto))
            return;

        _actionsSystem.RemoveAction(uid, new InstantAction(actionProto));
    }

    private void OnSericultureStart(EntityUid uid, SericultureComponent comp, SericultureActionEvent args)
    {
        if (_hungerSystem.IsHungerBelowState(uid, comp.MinHungerThreshold))
        {
            if (!_netManager.IsClient) /// It's gonna repeat the popup like 50 times otherwise, fun! Yes this does mean a noticable delay when doing it on the client but there's not much I can do here apart from cry.
                _popupSystem.PopupEntity(Loc.GetString(comp.PopupText), uid, uid);
            return;
        }

        var doAfter = new DoAfterArgs(uid, comp.ProductionLength, new SericultureDoAfterEvent(), uid)
        { // I'm not sure if more things should be put here, but imo ideally it should probably be set in the component/YAML. Not sure if this is currently possible.
            BreakOnUserMove = true,
            BlockDuplicate = true,
            BreakOnDamage = true,
            CancelDuplicate = true,
        };

        _doAfterSystem.TryStartDoAfter(doAfter);
    }
}

/// <summary>
/// Should be relayed upon using the action.
/// </summary>
public sealed class SericultureActionEvent : InstantActionEvent { }

/// <summary>
/// Is relayed at the end of the sericulturing doafter.
/// </summary>
[Serializable, NetSerializable]
public sealed class SericultureDoAfterEvent : SimpleDoAfterEvent { }

