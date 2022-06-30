using Content.Server.Morgue.Components;
using Content.Shared.Morgue;
using Content.Shared.Examine;
using Robust.Server.GameObjects;
using Content.Server.Popups;
using Content.Shared.Standing;
using Robust.Shared.Player;
using Robust.Shared.Audio;
using Content.Server.Storage.Components;
using Content.Server.Storage.EntitySystems;
using Content.Shared.Body.Components;
using Robust.Server.Containers;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Content.Shared.Interaction;

namespace Content.Server.Morgue;

/// <summary>
///     This is the system for morgues but is also used by extension
///     for this such as crematoriums. Anything with a slab that you stick
///     bodies into would work pretty well.
/// </summary>
public sealed class MorgueSystem : EntitySystem
{
    [Dependency] private readonly EntityStorageSystem _entityStorage = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SharedInteractionSystem _interactionSystem = default!;
    [Dependency] private readonly StandingStateSystem _standing = default!;
    [Dependency] private readonly ContainerSystem _container = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MorgueComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<MorgueComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<MorgueTrayComponent, StorageBeforeCloseEvent>(OnStorageBeforeClose);

        //These are used to intercept and override the entityStorage opening functionality.
        SubscribeLocalEvent<MorgueComponent, ActivateInWorldEvent>(ToggleMorgueSlab,
            before: new[] { typeof(EntityStorageSystem) });
        SubscribeLocalEvent<MorgueTrayComponent, ActivateInWorldEvent>(ToggleMorgueTray,
            before: new[] { typeof(EntityStorageSystem) });
    }

    /// <summary>
    ///     Initializes the tray container and make sure the tray is there.
    /// </summary>
    private void OnInit(EntityUid uid, MorgueComponent component, ComponentInit args)
    {
        if (TryComp<AppearanceComponent>(uid, out var app))
            app.SetData(MorgueVisuals.Open, false);

        component.TrayContainer = _container.EnsureContainer<ContainerSlot>(uid, "morgue_tray");
        component.TrayContainer.ShowContents = false;

        component.Tray = Spawn(component.TrayPrototypeId, Transform(uid).Coordinates);
        EnsureComp<MorgueTrayComponent>(component.Tray).Morgue = uid;

        component.TrayContainer.Insert(component.Tray);
    }

    /// <summary>
    ///     Toggles the morgue open and close.
    /// </summary>
    public void ToggleMorgueSlab(EntityUid uid, MorgueComponent component, ActivateInWorldEvent args)
    {
        if (args.Handled)
            return;
        args.Handled = true;

        if (component.Open)
        {
            CloseMorgue(uid, component);
        }
        else if (CanOpenSlab(uid, component))
        {
            OpenMorgue(uid, component);
        }
    }

    /// <summary>
    ///     Just a wrapper so if you click on the tray it closes the corresponding morgue
    /// </summary>
    private void ToggleMorgueTray(EntityUid uid, MorgueTrayComponent component, ActivateInWorldEvent args)
    {
        if (!TryComp<MorgueComponent>(component.Morgue, out var morb))
            return;

        ToggleMorgueSlab(component.Morgue, morb, args);
    }

    /// <summary>
    ///     Handles appearance changes, places the tray in the proper location,
    ///     and "opens" its container, allowing the contents to spill out.
    /// </summary>
    public void OpenMorgue(EntityUid uid, MorgueComponent component)
    {
        if (TryComp<AppearanceComponent>(uid, out var app))
        {
            app.SetData(MorgueVisuals.Open, true);
            app.SetData(MorgueVisuals.HasContents, false);
            app.SetData(MorgueVisuals.HasSoul, false);
            app.SetData(MorgueVisuals.HasMob, false);
        }

        component.TrayContainer.Remove(component.Tray, EntityManager);
        var trayXform = Transform(component.Tray);
        trayXform.Coordinates = new EntityCoordinates(uid, 0, -1);
        trayXform.WorldRotation = Transform(uid).WorldRotation;

        component.Open = true;

        _entityStorage.OpenStorage(component.Tray);
    }

    /// <summary>
    ///     Same as OpenMorgue(), except it closes it.
    /// </summary>
    public void CloseMorgue(EntityUid uid, MorgueComponent component)
    {
        component.Open = false;

        if (!TryComp<EntityStorageComponent>(component.Tray, out var storage))
            return;

        if (TryComp<AppearanceComponent>(uid, out var app))
            app.SetData(MorgueVisuals.Open, false);

        _entityStorage.CloseStorage(component.Tray, storage);

        component.TrayContainer.Insert(component.Tray, EntityManager);
        CheckContents(uid, component);
    }

    /// <summary>
    ///     Allows laying down bodies to enter into the morgue.
    ///     Note: it checks the tray because the tray is what actually
    ///     holds the bodies and such.
    /// </summary>
    private void OnStorageBeforeClose(EntityUid uid, MorgueTrayComponent component, StorageBeforeCloseEvent args)
    {
        if (!HasComp<MorgueTrayComponent>(uid))
            return;

        foreach (var ent in args.Contents)
        {
            if (HasComp<SharedBodyComponent>(ent) && !_standing.IsDown(ent))
                args.Contents.Remove(ent);
        }
    }

    /// <summary>
    ///     Handles the examination text for looking at a morgue.
    /// </summary>
    private void OnExamine(EntityUid uid, MorgueComponent component, ExaminedEvent args)
    {
        if (!TryComp<AppearanceComponent>(uid, out var appearance))
            return;

        if (!args.IsInDetailsRange)
            return;

        if (appearance.TryGetData(MorgueVisuals.HasSoul, out bool hasSoul) && hasSoul)
            args.PushMarkup(Loc.GetString("morgue-entity-storage-component-on-examine-details-body-has-soul"));
        else if (appearance.TryGetData(MorgueVisuals.HasMob, out bool hasMob) && hasMob)
            args.PushMarkup(Loc.GetString("morgue-entity-storage-component-on-examine-details-body-has-no-soul"));
        else if (appearance.TryGetData(MorgueVisuals.HasContents, out bool hasContents) && hasContents)
            args.PushMarkup(Loc.GetString("morgue-entity-storage-component-on-examine-details-has-contents"));
        else
            args.PushMarkup(Loc.GetString("morgue-entity-storage-component-on-examine-details-empty"));
    }

    /// <summary>
    ///     Updates data periodically in case something died/got deleted in the morgue.
    /// </summary>
    private void CheckContents(EntityUid uid, MorgueComponent? morgue = null)
    {
        if (!Resolve(uid, ref morgue))
            return;

        if (!TryComp<EntityStorageComponent>(morgue.Tray, out var storage))
            return;

        var hasMob = false;
        var hasSoul = false;

        foreach (var ent in storage.Contents.ContainedEntities)
        {
            if (!hasMob && HasComp<SharedBodyComponent>(ent))
                hasMob = true;
            if (!hasSoul && TryComp<ActorComponent?>(ent, out var actor) && actor.PlayerSession != null)
                hasSoul = true;
        }

        if (TryComp<AppearanceComponent>(uid, out var app))
        {
            app.SetData(MorgueVisuals.HasContents, storage.Contents.ContainedEntities.Count > 0);
            app.SetData(MorgueVisuals.HasMob, hasMob);
            app.SetData(MorgueVisuals.HasSoul, hasSoul);
        }
    }

    /// <summary>
    ///     Checks if there is room in front of the morgue to bring out the tray.
    /// </summary>
    /// <param name="uid">The morgue being opened</param>
    /// <param name="component"></param>
    /// <param name="silent">Whether or not a message is given when the morgue cannot be opened</param>
    /// <returns>Whether or not the tray can be opened</returns>
    public bool CanOpenSlab(EntityUid uid, MorgueComponent? component = null, bool silent = false)
    {
        if (!Resolve(uid, ref component))
            return false;

        if (!_interactionSystem.InRangeUnobstructed(uid,
                Transform(uid).Coordinates.Offset(Transform(uid).LocalRotation.GetCardinalDir().ToIntVec()),
                collisionMask: component.TrayCanOpenMask
            ))
        {
            if (!silent)
                _popup.PopupEntity(Loc.GetString("morgue-entity-storage-component-cannot-open-no-space"), uid, Filter.Pvs(uid));
            return false;
        }

        var ev = new StorageOpenAttemptEvent();
        RaiseLocalEvent(uid, ev, true);

        return !ev.Cancelled;
    }

    /// <summary>
    ///     Handles the periodic beeping that morgues do when a live body is inside.
    /// </summary>
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        foreach (var comp in EntityQuery<MorgueComponent>())
        {
            comp.AccumulatedFrameTime += frameTime;

            CheckContents(comp.Owner, comp);

            if (comp.AccumulatedFrameTime < comp.BeepTime)
                continue;

            comp.AccumulatedFrameTime -= comp.BeepTime;

            if (comp.DoSoulBeep && TryComp<AppearanceComponent>(comp.Owner, out var appearance) &&
                appearance.TryGetData(MorgueVisuals.HasSoul, out bool hasSoul) && hasSoul)
            {
                SoundSystem.Play(comp.OccupantHasSoulAlarmSound.GetSound(), Filter.Pvs(comp.Owner), comp.Owner);
            }
        }
    }
}
