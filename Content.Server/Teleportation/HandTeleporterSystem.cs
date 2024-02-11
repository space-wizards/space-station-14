using Content.Server.Administration.Logs;
using Content.Shared.DoAfter;
using Content.Shared.Database;
using Content.Shared.Interaction.Events;
using Content.Shared.Teleportation.Components;
using Content.Shared.Teleportation.Systems;
using Robust.Server.Audio;
using Robust.Server.GameObjects;

namespace Content.Server.Teleportation;

/// <summary>
/// This handles creating portals from a hand teleporter.
/// </summary>
public sealed class HandTeleporterSystem : EntitySystem
{
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly LinkedEntitySystem _link = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly SharedDoAfterSystem _doafter = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<HandTeleporterComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<HandTeleporterComponent, TeleporterDoAfterEvent>(OnDoAfter);
    }

    private void OnDoAfter(EntityUid uid, HandTeleporterComponent component, DoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        HandlePortalUpdating(uid, component, args.Args.User);

        args.Handled = true;
    }

    private void OnUseInHand(EntityUid uid, HandTeleporterComponent component, UseInHandEvent args)
    {
        if (Deleted(component.FirstPortal))
            component.FirstPortal = null;

        if (Deleted(component.SecondPortal))
            component.SecondPortal = null;

        if (component.FirstPortal != null && component.SecondPortal != null)
        {
            // handle removing portals immediately as opposed to a doafter
            HandlePortalUpdating(uid, component, args.User);
        }
        else
        {
            var xform = Transform(args.User);
            if (xform.ParentUid != xform.GridUid)
                return;

            var doafterArgs = new DoAfterArgs(EntityManager, args.User, component.PortalCreationDelay, new TeleporterDoAfterEvent(), uid, used: uid)
            {
                BreakOnDamage = true,
                BreakOnUserMove = true,
                MovementThreshold = 0.5f,
            };

            _doafter.TryStartDoAfter(doafterArgs);
        }
    }


    /// <summary>
    ///     Creates or removes a portal given the state of the hand teleporter.
    /// </summary>
    private void HandlePortalUpdating(EntityUid uid, HandTeleporterComponent component, EntityUid user)
    {
        if (Deleted(user))
            return;

        var xform = Transform(user);

        // Create the first portal.
        if (Deleted(component.FirstPortal) && Deleted(component.SecondPortal))
        {
            // don't portal
            if (xform.ParentUid != xform.GridUid)
                return;

            var timeout = EnsureComp<PortalTimeoutComponent>(user);
            timeout.EnteredPortal = null;
            component.FirstPortal = Spawn(component.FirstPortalPrototype, Transform(user).Coordinates);
            _adminLogger.Add(LogType.EntitySpawn, LogImpact.Low, $"{ToPrettyString(user):player} opened {ToPrettyString(component.FirstPortal.Value)} at {Transform(component.FirstPortal.Value).Coordinates} using {ToPrettyString(uid)}");
            _audio.PlayPvs(component.NewPortalSound, uid);
        }
        else if (Deleted(component.SecondPortal))
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
