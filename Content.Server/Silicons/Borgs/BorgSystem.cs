using Content.Server.Administration.Managers;
using Content.Server.Body.Systems;
using Content.Server.Ghost.Roles;
using Content.Server.Mind;
using Content.Server.Mind.Components;
using Content.Server.Players.PlayTimeTracking;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Silicons.Borgs;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.Throwing;
using Content.Shared.Wires;
using Robust.Server.Player;
using Robust.Shared.Containers;
using Robust.Shared.Random;

namespace Content.Server.Silicons.Borgs;

/// <inheritdoc/>
public sealed partial class BorgSystem : SharedBorgSystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly RoleBanManager _roleBan = default!;
    [Dependency] private readonly BodySystem _bobby = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly PlayTimeTrackingSystem _playTimeTracking = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BorgChassisComponent, AfterInteractUsingEvent>(OnChassisInteractUsing);
        SubscribeLocalEvent<BorgChassisComponent, EntInsertedIntoContainerMessage>(OnInserted);
        SubscribeLocalEvent<BorgChassisComponent, EntRemovedFromContainerMessage>(OnRemoved);
        SubscribeLocalEvent<BorgChassisComponent, MindAddedMessage>(OnMindAdded);
        SubscribeLocalEvent<BorgChassisComponent, MindRemovedMessage>(OnMindRemoved);

        SubscribeLocalEvent<BorgBrainComponent, MindAddedMessage>(OnBrainMindAdded);

        InitializeMMI();
    }

    private void OnChassisInteractUsing(EntityUid uid, BorgChassisComponent component, AfterInteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (TryComp<WiresPanelComponent>(uid, out var panel) && !panel.Open)
            return;

        args.Handled = true;
        var used = args.Used;

        if (component.BrainEntity == null &&
            HasComp<BorgBrainComponent>(used) &&
            component.BrainWhitelist?.IsValid(used) != false)
        {
            if (_mind.TryGetMind(used, out var mind) && mind.Session != null)
            {
                if (!CanPlayerBeBorgged(mind.Session, component))
                {
                    Popup.PopupEntity(Loc.GetString("borg-player-not-allowed"), used, args.User);
                    return;
                }
            }

            component.BrainContainer.Insert(used);
        }
    }

    // todo: consider transferring over the ghost role? managing that might suck.
    private void OnInserted(EntityUid uid, BorgChassisComponent component, EntInsertedIntoContainerMessage args)
    {
        if (HasComp<BorgBrainComponent>(args.Entity) && _mind.TryGetMind(args.Entity, out var mind))
        {
            _mind.TransferTo(mind, uid);
        }
    }

    private void OnRemoved(EntityUid uid, BorgChassisComponent component, EntRemovedFromContainerMessage args)
    {
        if (HasComp<BorgBrainComponent>(args.Entity) && _mind.TryGetMind(uid, out var mind))
        {
            _mind.TransferTo(mind, args.Entity);
        }
    }

    private void OnMindAdded(EntityUid uid, BorgChassisComponent component, MindAddedMessage args)
    {
        BorgStartup(uid, component);
    }

    private void OnMindRemoved(EntityUid uid, BorgChassisComponent component, MindRemovedMessage args)
    {
        BorgShutdown(uid, component);
    }

    private void OnBrainMindAdded(EntityUid uid, BorgBrainComponent component, MindAddedMessage args)
    {
        if (!Container.TryGetOuterContainer(uid, Transform(uid), out var container))
            return;

        var containerEnt = container.Owner;

        if (!TryComp<BorgChassisComponent>(containerEnt, out var chassisComponent) ||
            container.ID != chassisComponent.BrainContainerId)
            return;

        if (!_mind.TryGetMind(uid, out var mind) || mind.Session == null)
            return;

        if (!CanPlayerBeBorgged(mind.Session, chassisComponent))
        {
            Popup.PopupEntity(Loc.GetString("borg-player-not-allowed-eject"), uid);
            Container.RemoveEntity(containerEnt, uid);
            _throwing.TryThrow(uid, _random.NextVector2() * 5, 5f);
            return;
        }

        _mind.TransferTo(mind, containerEnt);
    }

    public void BorgStartup(EntityUid uid, BorgChassisComponent component)
    {
        Popup.PopupEntity(Loc.GetString("borg-mind-added", ("name", Identity.Name(uid, EntityManager))), uid);
    }

    public void BorgShutdown(EntityUid uid, BorgChassisComponent component)
    {
        Popup.PopupEntity(Loc.GetString("borg-mind-removed", ("name", Identity.Name(uid, EntityManager))), uid);

    }

    /// <summary>
    /// Checks that a player has fulfilled the requirements for the borg job.
    /// If they don't have enough hours, they cannot be placed into a chassis.
    /// </summary>
    public bool CanPlayerBeBorgged(IPlayerSession session, BorgChassisComponent component)
    {
        var disallowedJobs = _playTimeTracking.GetDisallowedJobs(session);

        if (disallowedJobs.Contains(component.BorgJobId))
            return false;

        if (_roleBan.GetJobBans(session.UserId)?.Contains(component.BorgJobId) == true)
            return false;

        return true;
    }
}
