using Content.Server.Administration.Logs;
using Content.Server.DoAfter;
using Content.Shared.Cluwne;
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

using System.Linq;


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
        _robustRandom.Shuffle(spawnLocations);

        foreach (var location in spawnLocations)
        {

            if (Deleted(user))
                return;

            // Create the first portal.
            if (component.FirstPortal == null && component.SecondPortal == null)
            {
                var xform = Transform(user);
                if (xform.ParentUid != xform.GridUid)
                    return;
                {
                    var timeout = EnsureComp<PortalTimeoutComponent>(user);
                    timeout.EnteredPortal = null;
                    var coords = Transform(location.Owner);
                    component.FirstPortal = Spawn(component.FirstPortalPrototype, coords.Coordinates);
                }

            }
            else if (component.SecondPortal == null)
            {
                var timeout = EnsureComp<PortalTimeoutComponent>(user);
                timeout.EnteredPortal = null;
                component.SecondPortal = Spawn(component.SecondPortalPrototype, Transform(user).Coordinates);
                _adminLogger.Add(LogType.EntitySpawn, LogImpact.Low, $"{ToPrettyString(user):player} opened {ToPrettyString(component.SecondPortal.Value)} at {Transform(component.SecondPortal.Value).Coordinates} linked to {ToPrettyString(component.FirstPortal!.Value)} using {ToPrettyString(uid)}");
                _link.TryLink(component.FirstPortal!.Value, component.SecondPortal.Value, true);
                _audio.PlayPvs(component.NewPortalSound, uid);
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
}
