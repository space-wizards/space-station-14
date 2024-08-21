using Content.Shared.ActionBlocker;
using Content.Shared.Burial;
using Content.Shared.Burial.Components;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Movement.Events;
using Content.Shared.Placeable;
using Content.Shared.Popups;
using Content.Shared.Storage.Components;
using Content.Shared.Storage.EntitySystems;
using Robust.Shared.Audio.Systems;

namespace Content.Server.Burial.Systems;

public sealed class BurialSystem : EntitySystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly SharedEntityStorageSystem _storageSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GraveComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<GraveComponent, ActivateInWorldEvent>(OnActivate);
        SubscribeLocalEvent<GraveComponent, AfterInteractUsingEvent>(OnAfterInteractUsing, before: new[] { typeof(PlaceableSurfaceSystem) });
        SubscribeLocalEvent<GraveComponent, GraveDiggingDoAfterEvent>(OnGraveDigging);

        SubscribeLocalEvent<GraveComponent, StorageOpenAttemptEvent>(OnOpenAttempt);
        SubscribeLocalEvent<GraveComponent, StorageCloseAttemptEvent>(OnCloseAttempt);
        SubscribeLocalEvent<GraveComponent, StorageAfterOpenEvent>(OnAfterOpen);
        SubscribeLocalEvent<GraveComponent, StorageAfterCloseEvent>(OnAfterClose);

        SubscribeLocalEvent<GraveComponent, ContainerRelayMovementEntityEvent>(OnRelayMovement);
    }

    private void OnInteractUsing(EntityUid uid, GraveComponent component, InteractUsingEvent args)
    {
        if (args.Handled || component.ActiveShovelDigging)
            return;

        if (TryComp<ShovelComponent>(args.Used, out var shovel))
        {
            var doAfterEventArgs = new DoAfterArgs(EntityManager, args.User, component.DigDelay / shovel.SpeedModifier, new GraveDiggingDoAfterEvent(), uid, target: args.Target, used: uid)
            {
                BreakOnMove = true,
                BreakOnDamage = true,
                NeedHand = true,
            };

            if (component.Stream == null)
                component.Stream = _audioSystem.PlayPredicted(component.DigSound, uid, args.User)?.Entity;

            if (!_doAfterSystem.TryStartDoAfter(doAfterEventArgs))
            {
                _audioSystem.Stop(component.Stream);
                return;
            }


            StartDigging(uid, args.User, args.Used, component);
        }
        else
        {
            _popupSystem.PopupClient(Loc.GetString("grave-digging-requires-tool", ("grave", args.Target)), uid, args.User);
        }

        args.Handled = true;
    }

    private void OnAfterInteractUsing(EntityUid uid, GraveComponent component, AfterInteractUsingEvent args)
    {
        if (args.Handled)
            return;

        // don't place shovels on the grave, only dig
        if (HasComp<ShovelComponent>(args.Used))
            args.Handled = true;
    }

    private void OnActivate(EntityUid uid, GraveComponent component, ActivateInWorldEvent args)
    {
        if (args.Handled || !args.Complex)
            return;

        _popupSystem.PopupClient(Loc.GetString("grave-digging-requires-tool", ("grave", args.Target)), uid, args.User);
        args.Handled = true;
    }

    private void OnGraveDigging(EntityUid uid, GraveComponent component, GraveDiggingDoAfterEvent args)
    {
        if (args.Used != null)
        {
            component.ActiveShovelDigging = false;
            component.Stream = _audioSystem.Stop(component.Stream);
        }
        else
        {
            component.HandDiggingDoAfter = null;
        }

        if (args.Cancelled || args.Handled)
            return;

        component.DiggingComplete = true;

        if (args.Used != null)
            _storageSystem.ToggleOpen(args.User, uid);
        else
            _storageSystem.TryOpenStorage(args.User, uid); //can only claw out
    }

    private void StartDigging(EntityUid uid, EntityUid user, EntityUid? used, GraveComponent component)
    {
        if (used != null)
        {
            var selfMessage = Loc.GetString("grave-start-digging-user", ("grave", uid), ("tool", used));
            var othersMessage = Loc.GetString("grave-start-digging-others", ("user", user), ("grave", uid), ("tool", used));
            _popupSystem.PopupPredicted(selfMessage, othersMessage, user, user);
            component.ActiveShovelDigging = true;
            Dirty(uid, component);
        }
        else
        {
            _popupSystem.PopupClient(Loc.GetString("grave-start-digging-user-trapped", ("grave", uid)), user, user, PopupType.Medium);
        }
    }

    private void OnOpenAttempt(EntityUid uid, GraveComponent component, ref StorageOpenAttemptEvent args)
    {
        if (component.DiggingComplete)
            return;

        args.Cancelled = true;
    }

    private void OnCloseAttempt(EntityUid uid, GraveComponent component, ref StorageCloseAttemptEvent args)
    {
        if (component.DiggingComplete)
            return;

        args.Cancelled = true;
    }

    private void OnAfterOpen(EntityUid uid, GraveComponent component, ref StorageAfterOpenEvent args)
    {
        component.DiggingComplete = false;
    }

    private void OnAfterClose(EntityUid uid, GraveComponent component, ref StorageAfterCloseEvent args)
    {
        component.DiggingComplete = false;
    }

    private void OnRelayMovement(EntityUid uid, GraveComponent component, ref ContainerRelayMovementEntityEvent args)
    {
        // We track a separate doAfter here, as we want someone with a shovel to
        // be able to come along and help someone trying to claw their way out
        if (_doAfterSystem.IsRunning(component.HandDiggingDoAfter))
            return;

        if (!_actionBlocker.CanMove(args.Entity))
            return;

        var doAfterEventArgs = new DoAfterArgs(EntityManager, args.Entity, component.DigDelay / component.DigOutByHandModifier, new GraveDiggingDoAfterEvent(), uid, target: uid)
        {
            NeedHand = false,
            BreakOnMove = true,
            BreakOnHandChange = false,
            BreakOnDamage = false
        };


        if (component.Stream == null)
            component.Stream = _audioSystem.PlayPredicted(component.DigSound, uid, args.Entity)?.Entity;

        if (!_doAfterSystem.TryStartDoAfter(doAfterEventArgs, out component.HandDiggingDoAfter))
        {
            _audioSystem.Stop(component.Stream);
            return;
        }

        StartDigging(uid, args.Entity, null, component);
    }
}
