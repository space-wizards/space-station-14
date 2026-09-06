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
    [Dependency] private SharedPortalSystem _portal = default!;

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
        if (args.Cancelled)
            return;

        if (args.Handled)
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

        Dirty(uid, component);

        if (component.FirstPortal != null && component.SecondPortal != null)
        {
            // handle removing portals immediately as opposed to a doafter
            HandlePortalUpdating(uid, component, args.User);
            args.Handled = true;
            return;
        }

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
        args.Handled = true;
    }

    /// <summary>
    /// Checks if both portals of a teleporter are on same grid/map
    /// and if the teleporter allows that. if the portals are in an illegal state, it fizzles them.
    /// </summary>
    private void CheckPortals(Entity<HandTeleporterComponent> entity)
    {
        // no need to check nothing if there aren't 2 portals
        if (Deleted(entity.Comp.FirstPortal))
            return;

        if (Deleted(entity.Comp.SecondPortal))
            return;

        var portal1Xform = Transform(entity.Comp.FirstPortal!.Value);
        var portal2Xform = Transform(entity.Comp.SecondPortal!.Value);

        var sameGrid = portal1Xform.GridUid == portal2Xform.GridUid;
        var sameMap = portal1Xform.MapID == portal2Xform.MapID;

        if (!sameGrid && !entity.Comp.AllowPortalsOnDifferentGrids)
        {
            FizzlePortals(entity, null, false);
            return;
        }

        if (!sameMap && !entity.Comp.AllowPortalsOnDifferentMaps)
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

        if (Deleted(component.FirstPortal) && Deleted(component.SecondPortal))
        {
            CreateFirstPortal(uid, component, user, xform);
            return;
        }

        if (Deleted(component.SecondPortal))
        {
            CreateSecondPortal(uid, component, user, xform);
            return;
        }

        FizzlePortals((uid, component), user, false);
    }

    private void CreateFirstPortal(EntityUid uid, HandTeleporterComponent component, EntityUid user, TransformComponent xform)
    {
        if (xform.ParentUid != xform.GridUid)
            return;

        component.FirstPortal = Spawn(component.FirstPortalPrototype, Transform(user).Coordinates);
        _portal.SetPortalTimeout(user, component.FirstPortal.Value);
        Dirty(uid, component);
        ConfigurePortalMapTravel(component.FirstPortal, component);

        _adminLogger.Add(LogType.EntitySpawn, LogImpact.High, $"{ToPrettyString(user):player} opened {ToPrettyString(component.FirstPortal.Value)} at {Transform(component.FirstPortal.Value).Coordinates} using {ToPrettyString(uid)}");
        _audio.PlayPvs(component.NewPortalSound, uid);
    }

    private void CreateSecondPortal(EntityUid uid, HandTeleporterComponent component, EntityUid user, TransformComponent xform)
    {
        if (xform.ParentUid != xform.GridUid)
            return;

        if (!component.AllowPortalsOnDifferentGrids && xform.ParentUid != Transform(component.FirstPortal!.Value).ParentUid)
        {
            // Whoops. Fizzle time. Crime time too because yippee I'm not refactoring this logic right now (I started to, I'm not going to.)
            FizzlePortals((uid, component), user, true);
            return;
        }

        component.SecondPortal = Spawn(component.SecondPortalPrototype, Transform(user).Coordinates);
        _portal.SetPortalTimeout(user, component.SecondPortal.Value);
        Dirty(uid, component);
        ConfigurePortalMapTravel(component.SecondPortal, component);

        _adminLogger.Add(LogType.EntitySpawn, LogImpact.High, $"{ToPrettyString(user):player} opened {ToPrettyString(component.SecondPortal.Value)} at {Transform(component.SecondPortal.Value).Coordinates} linked to {ToPrettyString(component.FirstPortal!.Value)} using {ToPrettyString(uid)}");
        _link.TryLink(component.FirstPortal!.Value, component.SecondPortal.Value, true);
        _audio.PlayPvs(component.NewPortalSound, uid);
    }

    private void ConfigurePortalMapTravel(EntityUid? portal, HandTeleporterComponent component)
    {
        if (!component.AllowPortalsOnDifferentMaps)
            return;

        if (!TryComp<PortalComponent>(portal, out var portalComponent))
            return;

        portalComponent.CanTeleportToOtherMaps = true;
    }

    /// <summary>
    /// Deletes both portals of a teleporter
    /// </summary>
    /// <param name="entity">the teleporter entity</param>
    /// <param name="user">who deleted the portals</param>
    /// <param name="instability">if it should send an "instability" popup to the user</param>
    private void FizzlePortals(Entity<HandTeleporterComponent> entity, EntityUid? user, bool instability)
    {
        LogPortalClosure(entity, user);

        // Clear both portals
        if (!Deleted(entity.Comp.FirstPortal))
            QueueDel(entity.Comp.FirstPortal.Value);
        if (!Deleted(entity.Comp.SecondPortal))
            QueueDel(entity.Comp.SecondPortal.Value);

        entity.Comp.FirstPortal = null;
        entity.Comp.SecondPortal = null;
        Dirty(entity);
        _audio.PlayPvs(entity.Comp.ClearPortalsSound, entity);

        if (!instability)
            return;

        if (user == null)
            return;

        _popup.PopupEntity(Loc.GetString("handheld-teleporter-instability-fizzle"), entity, user.Value, PopupType.MediumCaution);
    }

    private void LogPortalClosure(Entity<HandTeleporterComponent> entity, EntityUid? user)
    {
        var portalStrings = "";
        portalStrings += ToPrettyString(entity.Comp.FirstPortal);
        if (portalStrings != "")
            portalStrings += " and ";
        portalStrings += ToPrettyString(entity.Comp.SecondPortal);

        if (portalStrings == "")
            return;

        if (user != null)
        {
            _adminLogger.Add(LogType.EntityDelete, LogImpact.High, $"{ToPrettyString(user):player} closed {portalStrings} with {ToPrettyString(entity)}");
            return;
        }

        _adminLogger.Add(LogType.EntityDelete, LogImpact.High, $"{portalStrings} were closed");
    }
}
