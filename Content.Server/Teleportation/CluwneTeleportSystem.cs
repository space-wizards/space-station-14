using Content.Server.Administration.Logs;
using Content.Server.DoAfter;
using Content.Shared.Cluwne;
using Content.Server.Popups;
using Content.Shared.DoAfter;
using Content.Shared.Database;
using Content.Server.Anomaly;
using Content.Shared.Interaction.Events;
using Content.Shared.Teleportation.Components;
using Content.Shared.Teleportation.Systems;
using Robust.Server.GameObjects;
using Robust.Shared.Map.Components;
using Robust.Shared.Random;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.CCVar;
using Content.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics;
using Robust.Shared.Configuration;
using Content.Server.StationEvents.Components;
using Content.Shared.Materials;
using System.Linq;
using Content.Shared.Destructible;
using Content.Server.Construction.Completions;
using Content.Server.Anomaly.Components;
using Content.Server.Materials;
using Robust.Shared.Timing;
using Content.Server.Chat.Managers;
using Content.Server.Database;
using Robust.Server.Player;
using Content.Server.Chat.Systems;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Content.Server.Teleportation;

/// <summary>
/// This handles creating cluwne portals from a hand teleporter.
/// </summary>
public sealed class CluwneTeleportSystem : EntitySystem
{
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly LinkedEntitySystem _link = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly DoAfterSystem _doafter = default!;
    [Dependency] private readonly IRobustRandom _robustRandom = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly ChatSystem _chat = default!;


    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<CluwneTeleporterComponent, UseInHandEvent>(OnUseHand);
        SubscribeLocalEvent<CluwneTeleporterComponent, DoAfterEvent>(DoAfter);
    }

    private void DoAfter(EntityUid uid, CluwneTeleporterComponent component, DoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        HandlePortalUpdate(uid, component, args.Args.User);

        args.Handled = true;
    }

    private void OnUseHand(EntityUid uid, CluwneTeleporterComponent component, UseInHandEvent args)
    {
        if (_timing.CurTime < component.CooldownEndTime)
        {
            _popupSystem.PopupEntity(Loc.GetString("spell-gate-wait"), args.User);
            return;
        }

        if (Deleted(component.FirstPortal))
            component.FirstPortal = null;

        if (Deleted(component.SecondPortal))
            component.SecondPortal = null;

        if (component.FirstPortal != null && component.SecondPortal != null)
        {
            // handle removing portals immediately as opposed to a doafter
            HandlePortalUpdate(uid, component, args.User);
        }

        if (component.FirstPortal != null && component.SecondPortal != null)
        {
            // handle removing portals immediately as opposed to a doafter
            HandlePortalUpdate(uid, component, args.User);
        }
        else
        {
            var xform = Transform(args.User);
            if (xform.ParentUid != xform.GridUid)
                return;

            var doafterArgs = new DoAfterEventArgs(args.User, component.PortalCreationDelay, used: uid)
            {
                BreakOnDamage = true,
                BreakOnStun = true,
                BreakOnUserMove = true,
                MovementThreshold = 0.5f,
            };

            _doafter.DoAfter(doafterArgs);
        }
    }

    /// <summary>
    ///     Creates or removes a portal given the state of the hand teleporter.
    /// </summary>
    private void HandlePortalUpdate(EntityUid uid, CluwneTeleporterComponent component, EntityUid user)
    {
        var spawnLocations = EntityManager.EntityQuery<VentCritterSpawnLocationComponent>().ToList();
        var spawnAmount = (component.SpawnAmount);
        _robustRandom.Shuffle(spawnLocations);;

        if (Deleted(user))
            return;

        // Create the first portal.
        if (component.FirstPortal == null && component.SecondPortal == null)
        {

            foreach (var location in spawnLocations)
            {

                if (spawnAmount-- == 0)
                    break;

                var xform = Transform(user);
                if (xform.ParentUid != xform.GridUid)
                    return;
                {
                    var timeout = EnsureComp<PortalTimeoutComponent>(user);
                    timeout.EnteredPortal = null;
                    var coords = Transform(location.Owner);
                    component.FirstPortal = Spawn(component.FirstPortalPrototype, coords.Coordinates);
                    component.SecondPortal = Spawn(component.SecondPortalPrototype, Transform(user).Coordinates);
                    _adminLogger.Add(LogType.EntitySpawn, LogImpact.Low, $"{ToPrettyString(user):player} opened {ToPrettyString(component.SecondPortal.Value)} at {Transform(component.SecondPortal.Value).Coordinates} linked to {ToPrettyString(component.FirstPortal!.Value)} using {ToPrettyString(uid)}");
                    _link.TryLink(component.FirstPortal!.Value, component.SecondPortal.Value, true);
                    _audio.PlayPvs(component.NewPortalSound, uid);
                    _chat.TrySendInGameICMessage(user, Loc.GetString("spell-gate-speech"), InGameICChatType.Speak, false);
                    component.CooldownEndTime = _timing.CurTime + component.CooldownLength;
                }

            }

        }

        else
        {
            // Logging
            var portalStrings = "";
            portalStrings += ToPrettyString(component.FirstPortal!.Value);
            if (portalStrings != "")
                portalStrings += " and ";
                portalStrings += ToPrettyString(component.SecondPortal!.Value);
            if (portalStrings != "")
                _adminLogger.Add(LogType.EntityDelete, LogImpact.Low, $"{ToPrettyString(user):player} closed {portalStrings} with {ToPrettyString(uid)}");

                // Clear both portals
            QueueDel(component.FirstPortal!.Value);
            QueueDel(component.SecondPortal!.Value);

            component.FirstPortal = null;
            component.SecondPortal = null;
            _audio.PlayPvs(component.ClearPortalsSound, uid);
        }

    }
}
