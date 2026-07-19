using Content.Shared.Popups;
using Content.Shared.Actions;
using Content.Shared.Actions.Events;
using Content.Shared.Alert;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.IdentityManagement;
using Content.Shared.Maps;
using Content.Shared.Paper;
using Content.Shared.Physics;
using Content.Shared.Speech.Muting;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Shared.Abilities.Mime;

public sealed partial class MimePowersSystem : EntitySystem
{
    private static readonly EntityTimerId RepentTimer = new("repent");

    [Dependency] private SharedPopupSystem _popupSystem = default!;
    [Dependency] private SharedActionsSystem _actionsSystem = default!;
    [Dependency] private AlertsSystem _alertsSystem = default!;
    [Dependency] private TurfSystem _turf = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IEntityTimerManager _timers = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MimePowersComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<MimePowersComponent, ComponentShutdown>(OnComponentShutdown);
        SubscribeLocalEvent<MimePowersComponent, InvisibleWallActionEvent>(OnInvisibleWall);
        SubscribeLocalEvent<MimePowersComponent, ComponentHandleState>(OnHandleState);
        SubscribeLocalEvent<MimePowersComponent, EntityTimerEvent>(OnRepentTimer);

        SubscribeLocalEvent<MimePowersComponent, BreakVowAlertEvent>(OnBreakVowAlert);
        SubscribeLocalEvent<MimePowersComponent, RetakeVowAlertEvent>(OnRetakeVowAlert);
    }

    private void OnHandleState(Entity<MimePowersComponent> ent, ref ComponentHandleState args)
    {
        ScheduleRepent(ent);
    }

    private void OnRepentTimer(Entity<MimePowersComponent> ent, ref EntityTimerEvent args)
    {
        if (args.Id != RepentTimer || !ent.Comp.VowBroken || ent.Comp.ReadyToRepent)
            return;

        ent.Comp.ReadyToRepent = true;
        Dirty(ent);
        _popupSystem.PopupEntity(Loc.GetString("mime-ready-to-repent"), ent, ent);
    }

    private void OnComponentInit(Entity<MimePowersComponent> ent, ref ComponentInit args)
    {
        EnsureComp<MutedComponent>(ent);

        if (ent.Comp.PreventWriting)
        {
            EnsureComp<BlockWritingComponent>(ent, out var illiterateComponent);
            illiterateComponent.FailWriteMessage = ent.Comp.FailWriteMessage;
            Dirty(ent, illiterateComponent);
        }

        _alertsSystem.ShowAlert(ent.Owner, ent.Comp.VowAlert);
        _actionsSystem.AddAction(ent, ref ent.Comp.InvisibleWallActionEntity, ent.Comp.InvisibleWallAction);
    }

    private void OnComponentShutdown(Entity<MimePowersComponent> ent, ref ComponentShutdown args)
    {
        _actionsSystem.RemoveAction(ent.Owner, ent.Comp.InvisibleWallActionEntity);
    }

    /// <summary>
    /// Creates an invisible wall in a free space after some checks.
    /// </summary>
    private void OnInvisibleWall(Entity<MimePowersComponent> ent, ref InvisibleWallActionEvent args)
    {
        if (!ent.Comp.Enabled)
            return;

        if (_container.IsEntityOrParentInContainer(ent))
            return;

        var xform = Transform(ent);
        // Get the tile in front of the mime
        var offsetValue = xform.LocalRotation.ToWorldVec();
        var coords = xform.Coordinates.Offset(offsetValue).SnapToGrid(EntityManager);
        var tile = _turf.GetTileRef(coords);
        if (tile == null)
            return;

        // Check if the tile is blocked by a wall or mob, and don't create the wall if so
        if (_turf.IsTileBlocked(tile.Value, CollisionGroup.Impassable | CollisionGroup.Opaque))
        {
            _popupSystem.PopupEntity(Loc.GetString("mime-invisible-wall-failed"), ent, ent);
            return;
        }

        var messageSelf = Loc.GetString("mime-invisible-wall-popup-self", ("mime", Identity.Entity(ent.Owner, EntityManager)));
        var messageOthers = Loc.GetString("mime-invisible-wall-popup-others", ("mime", Identity.Entity(ent.Owner, EntityManager)));
        _popupSystem.PopupEntity(messageSelf, messageOthers, ent, ent);

        // Make sure we set the invisible wall to despawn properly
        PredictedSpawnAtPosition(ent.Comp.WallPrototype, _turf.GetTileCenter(tile.Value));
        // Handle args so cooldown works
        args.Handled = true;
    }

    private void OnBreakVowAlert(Entity<MimePowersComponent> ent, ref BreakVowAlertEvent args)
    {
        if (args.Handled)
            return;

        BreakVow(ent, ent);
        args.Handled = true;
    }

    private void OnRetakeVowAlert(Entity<MimePowersComponent> ent, ref RetakeVowAlertEvent args)
    {
        if (args.Handled)
            return;

        RetakeVow(ent, ent);
        args.Handled = true;
    }

    /// <summary>
    /// Break this mime's vow to not speak.
    /// </summary>
    public void BreakVow(EntityUid uid, MimePowersComponent? mimePowers = null)
    {
        if (!Resolve(uid, ref mimePowers))
            return;

        if (mimePowers.VowBroken)
            return;

        mimePowers.Enabled = false;
        mimePowers.VowBroken = true;
        mimePowers.VowRepentTime = _timing.CurTime + mimePowers.VowCooldown;
        Dirty(uid, mimePowers);
        _timers.SetTimerAt<MimePowersComponent>((uid, mimePowers), RepentTimer, mimePowers.VowRepentTime);
        RemComp<MutedComponent>(uid);
        if (mimePowers.PreventWriting)
            RemComp<BlockWritingComponent>(uid);

        _alertsSystem.ClearAlert(uid, mimePowers.VowAlert);
        _alertsSystem.ShowAlert(uid, mimePowers.VowBrokenAlert);
        _actionsSystem.RemoveAction(uid, mimePowers.InvisibleWallActionEntity);
    }

    /// <summary>
    /// Retake this mime's vow to not speak.
    /// </summary>
    public void RetakeVow(EntityUid uid, MimePowersComponent? mimePowers = null)
    {
        if (!Resolve(uid, ref mimePowers))
            return;

        if (!mimePowers.ReadyToRepent)
        {
            _popupSystem.PopupEntity(Loc.GetString("mime-not-ready-repent"), uid, uid);
            return;
        }

        mimePowers.Enabled = true;
        mimePowers.ReadyToRepent = false;
        mimePowers.VowBroken = false;
        Dirty(uid, mimePowers);
        _timers.CancelTimer<MimePowersComponent>(uid, RepentTimer);
        AddComp<MutedComponent>(uid);
        if (mimePowers.PreventWriting)
        {
            EnsureComp<BlockWritingComponent>(uid, out var illiterateComponent);
            illiterateComponent.FailWriteMessage = mimePowers.FailWriteMessage;
            Dirty(uid, illiterateComponent);
        }

        _alertsSystem.ClearAlert(uid, mimePowers.VowBrokenAlert);
        _alertsSystem.ShowAlert(uid, mimePowers.VowAlert);
        _actionsSystem.AddAction(uid, ref mimePowers.InvisibleWallActionEntity, mimePowers.InvisibleWallAction, uid);
    }

    private void ScheduleRepent(Entity<MimePowersComponent> ent)
    {
        if (ent.Comp.VowBroken && !ent.Comp.ReadyToRepent)
            _timers.SetTimerAt(ent, RepentTimer, ent.Comp.VowRepentTime);
        else
            _timers.CancelTimer<MimePowersComponent>(ent, RepentTimer);
    }
}
