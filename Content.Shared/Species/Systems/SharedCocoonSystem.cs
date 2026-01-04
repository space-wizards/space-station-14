using Content.Shared.Actions;
using Content.Shared.ActionBlocker;
using Content.Shared.Containers.Components;
using Content.Shared.DoAfter;
using Content.Shared.Hands.Components;
using Content.Shared.Humanoid;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Stunnable;
using Content.Shared.Bed.Sleep;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Species.Arachnid;

public abstract class SharedCocoonSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly IPrototypeManager _protoManager = default!;
    [Dependency] protected readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] protected readonly HungerSystem _hunger = default!;
    [Dependency] protected readonly SharedPopupSystem _popups = default!;
    [Dependency] protected readonly ActionBlockerSystem _blocker = default!;
    [Dependency] protected readonly SharedAudioSystem _audio = default!;
    [Dependency] protected readonly SharedContainerSystem _container = default!;
    [Dependency] protected readonly MobStateSystem _mobState = default!;
    [Dependency] protected readonly INetManager _netMan = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CocoonerComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<CocoonerComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<CocoonerComponent, WrapActionEvent>(OnWrapAction);
    }

    private void OnMapInit(EntityUid uid, CocoonerComponent component, MapInitEvent args)
    {
        // Check if the action prototype exists (test-safe)
        if (component.WrapAction != default && !_protoManager.TryIndex<EntityPrototype>(component.WrapAction, out _))
            return;

        _actions.AddAction(uid, ref component.ActionEntity, component.WrapAction, container: uid);
    }

    private void OnShutdown(EntityUid uid, CocoonerComponent component, ComponentShutdown args)
    {
        _actions.RemoveAction(uid, component.ActionEntity);
    }

    private void OnWrapAction(EntityUid uid, CocoonerComponent component, ref WrapActionEvent args)
    {
        if (args.Handled)
            return;

        var user = args.Performer;
        var target = args.Target;

        if (target == user)
            return;

        if (!HasComp<HumanoidAppearanceComponent>(target))
        {
            _popups.PopupEntity(Loc.GetString("arachnid-wrap-invalid-target"), user, user);
            return;
        }

        // Check if target is already in a cocoon container
        if (_container.TryGetContainingContainer(target, out var existingContainer) &&
            HasComp<CocoonContainerComponent>(existingContainer.Owner))
        {
            _popups.PopupEntity(Loc.GetString("arachnid-wrap-already"), user, user);
            return;
        }

        if (!_blocker.CanInteract(user, target))
            return;

        // Check if entity has enough hunger to perform the action
        if (TryComp<Content.Shared.Nutrition.Components.HungerComponent>(user, out var hungerComp))
        {
            var currentHunger = _hunger.GetHunger(hungerComp);
            if (currentHunger < component.HungerCost)
            {
                _popups.PopupEntity(Loc.GetString("arachnid-wrap-failure-hunger"), user, user);
                return;
            }
        }

        // Only require hands if the entity has hands (spiders don't have hands)
        var needHand = HasComp<HandsComponent>(user);

        var wrapTime = component.WrapDuration;
        // Reduce DoAfter time if target is stunned, asleep, critical, or dead
        if (HasComp<StunnedComponent>(target) || HasComp<SleepingComponent>(target) || _mobState.IsCritical(target) || _mobState.IsDead(target))
            wrapTime = component.WrapDuration_Short;

        var doAfter = new DoAfterArgs(EntityManager, user, TimeSpan.FromSeconds(wrapTime), new WrapDoAfterEvent(), user, target)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = needHand,
            DistanceThreshold = component.WrapRange,
            CancelDuplicate = true,
            BlockDuplicate = true,
        };

        if (!_doAfter.TryStartDoAfter(doAfter))
            return;

        // Server-only operations (admin logs, etc.)
        if (!_netMan.IsClient)
        {
            OnWrapActionServer(user, target);
        }

        _audio.PlayPredicted(new SoundPathSpecifier("/Audio/Items/Handcuffs/rope_start.ogg"), target, user);

        _popups.PopupEntity(Loc.GetString("arachnid-wrap-start-user", ("target", target)), user, user);
        _popups.PopupEntity(Loc.GetString("arachnid-wrap-start-target", ("user", user)), target, target, PopupType.LargeCaution);

        args.Handled = true;
    }

    /// <summary>
    /// Server-only operations for wrap action (admin logs, etc.)
    /// </summary>
    protected virtual void OnWrapActionServer(EntityUid user, EntityUid target)
    {
        // Override in server system to add admin logs and other server-only operations
    }
}

public sealed partial class WrapActionEvent : EntityTargetActionEvent
{
}

public sealed partial class UnwrapActionEvent : EntityTargetActionEvent
{
}

[Serializable, NetSerializable]
public sealed partial class WrapDoAfterEvent : SimpleDoAfterEvent
{
}

[Serializable, NetSerializable]
public sealed partial class UnwrapDoAfterEvent : SimpleDoAfterEvent
{
}
