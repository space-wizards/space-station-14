using Content.Server.Administration.Logs;
using Content.Server.Popups;
using Content.Shared.DoAfter;
using Content.Shared.Database;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Content.Shared.Teleportation.Components;
using Content.Shared.Teleportation.Systems;
using Robust.Server.Audio;

namespace Content.Server.Teleportation;

/// <summary>
/// This handles creating portals from a hand teleporter.
/// </summary>
public sealed partial class HandTeleporterSystem : EntitySystem
{
    [Dependency] private IAdminLogManager _adminLogger = default!;
    [Dependency] private LinkedEntitySystem _link = default!;
    [Dependency] private AudioSystem _audio = default!;
    [Dependency] private SharedDoAfterSystem _doafter = default!;
    [Dependency] private PopupSystem _popup = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<HandTeleporterComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<HandTeleporterComponent, TeleporterDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<GridSplitEvent>(OnGridSplit);
    }

    private void OnGridSplit(ref GridSplitEvent args)
    {
        var teleporterQuery = EntityQueryEnumerator<HandTeleporterComponent>();
        while (teleporterQuery.MoveNext(out var uid, out var teleporter))
        {
            CheckPortals((uid, teleporter));
        }
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
        if (args.Handled)
            return;

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
                BreakOnMove = true,
                MovementThreshold = 0.5f,
            };

            _doafter.TryStartDoAfter(doafterArgs);
        }

        args.Handled = true;
    }

    /// <summary>
    /// Checks if both portals of a teleporter are on same grid/map
    /// and if the teleporter allows that. if the portals are in an illegal state, it fizzles them.
    /// </summary>
    private void CheckPortals(Entity<HandTeleporterComponent> entity)
    {
        // no need to check nothing if there aren't 2 portals
        if (Deleted(entity.Comp.FirstPortal) || Deleted(entity.Comp.SecondPortal))
            return;

        var portal1Xform = Transform(entity.Comp.FirstPortal!.Value);
        var portal2Xform = Transform(entity.Comp.SecondPortal!.Value);

        var sameGrid = portal1Xform.GridUid == portal2Xform.GridUid;
        var sameMap = portal1Xform.MapID == portal2Xform.MapID;

        if (!sameGrid && !entity.Comp.AllowPortalsOnDifferentGrids || !sameMap && !entity.Comp.AllowPortalsOnDifferentMaps)
            FizzlePortals(entity, null, false);
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

            if (component.AllowPortalsOnDifferentMaps && TryComp<PortalComponent>(component.FirstPortal, out var portal))
                portal.CanTeleportToOtherMaps = true;

            _adminLogger.Add(LogType.EntitySpawn, LogImpact.High, $"{ToPrettyString(user):player} opened {ToPrettyString(component.FirstPortal.Value)} at {Transform(component.FirstPortal.Value).Coordinates} using {ToPrettyString(uid)}");
            _audio.PlayPvs(component.NewPortalSound, uid);
        }
        else if (Deleted(component.SecondPortal))
        {
            if (xform.ParentUid != xform.GridUid) // Still, don't portal.
                return;

            if (!component.AllowPortalsOnDifferentGrids && xform.ParentUid != Transform(component.FirstPortal!.Value).ParentUid)
            {
                // Whoops. Fizzle time. Crime time too because yippee I'm not refactoring this logic right now (I started to, I'm not going to.)
                FizzlePortals((uid, component), user, true);
                return;
            }

            var timeout = EnsureComp<PortalTimeoutComponent>(user);
            timeout.EnteredPortal = null;
            component.SecondPortal = Spawn(component.SecondPortalPrototype, Transform(user).Coordinates);

            if (component.AllowPortalsOnDifferentMaps && TryComp<PortalComponent>(component.SecondPortal, out var portal))
                portal.CanTeleportToOtherMaps = true;

            _adminLogger.Add(LogType.EntitySpawn, LogImpact.High, $"{ToPrettyString(user):player} opened {ToPrettyString(component.SecondPortal.Value)} at {Transform(component.SecondPortal.Value).Coordinates} linked to {ToPrettyString(component.FirstPortal!.Value)} using {ToPrettyString(uid)}");
            _link.TryLink(component.FirstPortal!.Value, component.SecondPortal.Value, true);
            _audio.PlayPvs(component.NewPortalSound, uid);
        }
        else
        {
            FizzlePortals((uid, component), user, false);
        }
    }

    /// <summary>
    /// Deletes both portals of a teleporter
    /// </summary>
    /// <param name="entity">the teleporter entity</param>
    /// <param name="user">who deleted the portals</param>
    /// <param name="instability">if it should send an "instability" popup to the user</param>
    private void FizzlePortals(Entity<HandTeleporterComponent> entity, EntityUid? user, bool instability)
    {
        // Logging
        var portalStrings = "";
        portalStrings += ToPrettyString(entity.Comp.FirstPortal);
        if (portalStrings != "")
            portalStrings += " and ";
        portalStrings += ToPrettyString(entity.Comp.SecondPortal);
        if (portalStrings != "")
        {
            if (user != null)
                _adminLogger.Add(LogType.EntityDelete, LogImpact.High, $"{ToPrettyString(user):player} closed {portalStrings} with {ToPrettyString(entity)}");
            else
                _adminLogger.Add(LogType.EntityDelete, LogImpact.High, $"{portalStrings} were closed");
        }

        // Clear both portals
        if (!Deleted(entity.Comp.FirstPortal))
            QueueDel(entity.Comp.FirstPortal.Value);
        if (!Deleted(entity.Comp.SecondPortal))
            QueueDel(entity.Comp.SecondPortal.Value);

        entity.Comp.FirstPortal = null;
        entity.Comp.SecondPortal = null;
        _audio.PlayPvs(entity.Comp.ClearPortalsSound, entity);

        if (instability && user != null)
            _popup.PopupEntity(Loc.GetString("handheld-teleporter-instability-fizzle"), entity, user.Value, PopupType.MediumCaution);
    }
}
