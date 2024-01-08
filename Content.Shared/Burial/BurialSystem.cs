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
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Server.Burial.Systems;

public sealed class BurialSystem : EntitySystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly SharedEntityStorageSystem _storageSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly INetManager _netManager = default!;

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
        SubscribeLocalEvent<ActiveGraveComponent, ComponentShutdown>(OnActiveGraveShutdown);
    }

    private void OnInteractUsing(EntityUid uid, GraveComponent component, InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        // allows someone to help you dig yourself out
        if (TryComp<ActiveGraveComponent>(uid, out var active) && !active.DiggingSelfOut)
            return;

        if (TryComp<ShovelComponent>(args.Used, out var shovel))
        {
            _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, args.User, component.DigDelay / shovel.SpeedModifier, new GraveDiggingDoAfterEvent(), uid, target: args.Target, used: uid)
            {
                BreakOnTargetMove = true,
                BreakOnUserMove = true,
                BreakOnDamage = true,
                NeedHand = true,
                BreakOnHandChange = true
            });

            StartDigging(uid, args.User, args.Used, component);
        }
        else
        {
            _popupSystem.PopupClient(Loc.GetString("grave-digging-requires-tool", ("grave", args.Target)), uid, args.User);
        }
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
        if (args.Handled)
            return;

        _popupSystem.PopupClient(Loc.GetString("grave-digging-requires-tool", ("grave", args.Target)), uid, args.User);
    }

    private void OnGraveDigging(EntityUid uid, GraveComponent component, GraveDiggingDoAfterEvent args)
    {
        RemComp<ActiveGraveComponent>(uid);

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
        ActiveGraveComponent activeComp = EnsureComp<ActiveGraveComponent>(uid);

        if (used != null)
        {
            _popupSystem.PopupClient(Loc.GetString("grave-start-digging-user", ("grave", uid), ("tool", used)), user, user);
            _popupSystem.PopupEntity(Loc.GetString("grave-start-digging-others", ("user", user), ("grave", uid), ("tool", used)), user, Filter.PvsExcept(user), true);
            if (_netManager.IsServer)
                activeComp.Stream = _audioSystem.PlayPvs(component.DigSound, uid).Value.Entity;
        }
        else
        {
            activeComp.DiggingSelfOut = true;
            _popupSystem.PopupClient(Loc.GetString("grave-start-digging-user-trapped", ("grave", uid)), user, user, PopupType.Medium);
        }
    }

    private void OnActiveGraveShutdown(EntityUid uid, ActiveGraveComponent component, ComponentShutdown args)
    {
        component.Stream = _audioSystem.Stop(component.Stream);
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
        if (!HasComp<ActiveGraveComponent>(uid))
        {
            _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, args.Entity, component.DigDelay / component.DigOutByHandModifier, new GraveDiggingDoAfterEvent(), uid, target: uid)
            {
                NeedHand = false,
                BreakOnUserMove = true,
                BreakOnTargetMove = false,
                BreakOnHandChange = false,
                BreakOnDamage = false
            });

            StartDigging(uid, args.Entity, null, component);
        }
    }
}
