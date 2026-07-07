using System.Linq;
using Content.Server.EUI;
using Content.Server.Ghost.Roles.Components;
using Content.Server.Humanoid.Systems;
using Content.Shared.Ghost;
using Content.Shared.Ghost.Roles;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Roles;
using Robust.Shared.Enums;
using Robust.Shared.Player;

namespace Content.Server.Ghost.Roles;

/// <summary>
/// Coordinates ghost role parties: linked groups of ghost roles that enter the
/// world together (e.g. the adventuring party). A controller entity places one
/// marker per member, claiming players are held in a waiting dialog, and the
/// entire party spawns simultaneously once every slot is claimed.
/// Cancelling, disconnecting, or otherwise ceasing to be a ghost observer
/// releases the slot and respawns a fresh marker, which re-registers the ghost
/// role with its default raffle settings.
/// </summary>
public sealed class GhostRolePartySystem : EntitySystem
{
    [Dependency] private readonly EuiManager _eui = default!;
    [Dependency] private readonly RandomHumanoidSystem _randomHumanoid = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly SharedRoleSystem _role = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GhostRolePartyControllerComponent, MapInitEvent>(OnControllerMapInit);
        SubscribeLocalEvent<GhostRolePartyControllerComponent, ComponentShutdown>(OnControllerShutdown);
        SubscribeLocalEvent<GhostRolePartySpawnerComponent, TakeGhostRoleEvent>(OnTakeRole);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // Waiting players must stay ghost observers. Taking another ghost role,
        // being revived into their old body, or leaving the game all forcibly
        // release their slot. Controllers are rare and tiny, so polling is cheap.
        var query = EntityQueryEnumerator<GhostRolePartyControllerComponent>();
        while (query.MoveNext(out var uid, out var controller))
        {
            if (controller.Spawning)
                continue;

            foreach (var slot in controller.Slots)
            {
                if (slot.Session is { } session && !IsValidWaiter(session))
                    Cancel(uid, session);
            }
        }
    }

    private void OnControllerMapInit(Entity<GhostRolePartyControllerComponent> ent, ref MapInitEvent args)
    {
        var coordinates = Transform(ent).Coordinates;
        foreach (var proto in ent.Comp.Members)
        {
            var slot = new GhostRolePartySlot(proto, coordinates);
            ent.Comp.Slots.Add(slot);
            SpawnMarker(ent, slot);
        }

        if (ent.Comp.Slots.Count == 0)
        {
            Log.Error($"Ghost role party controller {ToPrettyString(ent)} has no members configured, deleting.");
            QueueDel(ent);
        }
    }

    private void OnControllerShutdown(Entity<GhostRolePartyControllerComponent> ent, ref ComponentShutdown args)
    {
        // Admin deleted the controller (or the party has spawned): clean up
        // outstanding markers and waiting dialogs.
        foreach (var slot in ent.Comp.Slots)
        {
            if (slot.Spawner is { } spawner)
                QueueDel(spawner);
            slot.Spawner = null;
            slot.Session = null;

            var eui = slot.Eui;
            slot.Eui = null;
            if (eui is { IsShutDown: false })
                eui.Close();
        }

        ent.Comp.Slots.Clear();
    }

    private void SpawnMarker(Entity<GhostRolePartyControllerComponent> controller, GhostRolePartySlot slot)
    {
        var marker = Spawn(slot.SpawnerProto, slot.Coordinates);
        if (!TryComp<GhostRolePartySpawnerComponent>(marker, out var spawner))
        {
            Log.Error($"Ghost role party member prototype {slot.SpawnerProto} has no GhostRolePartySpawnerComponent!");
            QueueDel(marker);
            return;
        }

        spawner.Controller = controller;
        slot.Spawner = marker;
        slot.Settings = spawner.Settings;
        slot.MindRoles = spawner.MindRoles;
        slot.FallbackName = spawner.FallbackName;
    }

    private void OnTakeRole(Entity<GhostRolePartySpawnerComponent> ent, ref TakeGhostRoleEvent args)
    {
        if (args.TookRole)
            return;

        if (ent.Comp.Controller is not { } controllerUid ||
            !TryComp<GhostRolePartyControllerComponent>(controllerUid, out var controller) ||
            controller.Spawning)
        {
            return;
        }

        var player = args.Player;
        var slot = controller.Slots.Find(s => s.Spawner == ent.Owner);
        if (slot == null || slot.Session != null)
            return;

        // One slot per player: don't let someone win two raffles of the same party.
        if (controller.Slots.Any(s => s.Session == player))
            return;

        slot.Session = player;
        slot.Spawner = null;
        // Deleting the marker unregisters the ghost role; a fresh marker is
        // spawned if the player cancels.
        QueueDel(ent);
        args.TookRole = true;

        var eui = new GhostRolePartyWaitingEui(this, controllerUid);
        slot.Eui = eui;
        _eui.OpenEui(eui, player);

        UpdateParty((controllerUid, controller));
    }

    /// <summary>
    /// Releases the slot claimed by the given player, closing their dialog and
    /// respawning the marker so the ghost role is offered again.
    /// </summary>
    public void Cancel(EntityUid controllerUid, ICommonSession session)
    {
        if (!TryComp<GhostRolePartyControllerComponent>(controllerUid, out var controller) ||
            controller.Spawning)
        {
            return;
        }

        var slot = controller.Slots.Find(s => s.Session == session);
        if (slot == null)
            return;

        slot.Session = null;
        var eui = slot.Eui;
        slot.Eui = null;
        if (eui is { IsShutDown: false })
            eui.Close();

        SpawnMarker((controllerUid, controller), slot);
        UpdateParty((controllerUid, controller));
    }

    /// <summary>
    /// Called when a waiting dialog closes for any reason (cancel button, window
    /// closed, disconnect). Treated as a cancel unless the party is spawning.
    /// </summary>
    public void OnEuiClosed(EntityUid controllerUid, ICommonSession session)
    {
        Cancel(controllerUid, session);
    }

    public GhostRolePartyWaitingEuiState GetWaitingState(EntityUid controllerUid)
    {
        var state = new GhostRolePartyWaitingEuiState();
        if (!TryComp<GhostRolePartyControllerComponent>(controllerUid, out var controller))
            return state;

        state.Total = controller.Slots.Count;
        state.Ready = controller.Slots.Count(s => s.Session != null);
        return state;
    }

    /// <summary>
    /// A player may only occupy a party slot while they're a connected ghost observer.
    /// </summary>
    private bool IsValidWaiter(ICommonSession session)
    {
        return session.Status == SessionStatus.InGame &&
               session.AttachedEntity is { } attached &&
               HasComp<GhostComponent>(attached);
    }

    private void UpdateParty(Entity<GhostRolePartyControllerComponent> ent)
    {
        var ready = ent.Comp.Slots.Count(s => s.Session != null);
        if (ready >= ent.Comp.Slots.Count)
        {
            SpawnParty(ent);
            return;
        }

        foreach (var slot in ent.Comp.Slots)
        {
            slot.Eui?.StateDirty();
        }
    }

    private void SpawnParty(Entity<GhostRolePartyControllerComponent> ent)
    {
        // Locked in: cancels and dialog-close messages are ignored from here on.
        ent.Comp.Spawning = true;

        // Close the dialogs before spawning so nobody can cancel at the last moment.
        foreach (var slot in ent.Comp.Slots)
        {
            var eui = slot.Eui;
            slot.Eui = null;
            if (eui is { IsShutDown: false })
                eui.Close();
        }

        foreach (var slot in ent.Comp.Slots)
        {
            // Last-moment sanity check: never yank someone who was revived or took
            // another role in the same tick the final slot filled.
            if (slot.Session is not { } session || !IsValidWaiter(session))
                continue;

            var mob = _randomHumanoid.SpawnRandomHumanoid(slot.Settings, slot.Coordinates, Loc.GetString(slot.FallbackName));
            _transform.AttachToGridOrMap(mob);

            // Mirrors GhostRoleSystem.GhostRoleInternalCreateMindAndTransfer,
            // which we can't use directly since the ghost role markers are gone.
            if (_mind.TryGetMind(session.UserId, out _, out var oldMind) && !oldMind.IsVisitingEntity)
                _mind.WipeMind(session);

            EnsureComp<MindContainerComponent>(mob);
            var newMind = _mind.CreateMind(session.UserId, MetaData(mob).EntityName);
            _mind.SetUserId(newMind, session.UserId);
            _mind.TransferTo(newMind, mob);
            _role.MindAddRoles(newMind.Owner, slot.MindRoles, newMind.Comp);
        }

        QueueDel(ent);
    }
}
