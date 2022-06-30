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
using System.Linq;
using Robust.Shared.Physics;

namespace Content.Server.Morgue;

public sealed class MorgueEntityStorageSystem : EntitySystem
{
    [Dependency] private readonly EntityStorageSystem _entityStorage = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SharedInteractionSystem _interactionSystem = default!;
    [Dependency] private readonly StandingStateSystem _stando = default!;
    [Dependency] private readonly ContainerSystem _container = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MorgueEntityStorageComponent, ComponentInit>(OnInit);

        SubscribeLocalEvent<MorgueEntityStorageComponent, ActivateInWorldEvent>(OnActivate,
            before: new[] { typeof(EntityStorageSystem) });
        SubscribeLocalEvent<MorgueTrayComponent, ActivateInWorldEvent>(OnTrayActivate,
            before: new[] { typeof(EntityStorageSystem) });
        SubscribeLocalEvent<MorgueEntityStorageComponent, ExaminedEvent>(OnExamine);
    }

    private void OnInit(EntityUid uid, MorgueEntityStorageComponent component, ComponentInit args)
    {
        if (TryComp<AppearanceComponent>(uid, out var app))
            app.SetData(MorgueVisuals.Open, false);

        component.TrayContainer = _container.EnsureContainer<ContainerSlot>(uid, "morgue_tray");
        component.TrayContainer.ShowContents = false;

        component.Tray = Spawn(component.TrayPrototypeId, Transform(uid).Coordinates);
        EnsureComp<MorgueTrayComponent>(component.Tray).Morgue = uid;
        component.TrayContainer.Insert(component.Tray);
    }

    private void OnActivate(EntityUid uid, MorgueEntityStorageComponent component, ActivateInWorldEvent args)
    {
        if (args.Handled)
            return;
        args.Handled = true;

        if (!TryComp<EntityStorageComponent>(uid, out var storage))
            return;

        if (storage.Open)
        {
            CloseMorgue(uid, component);
        }
        else if (CanOpen(args.User, uid, component))
        {
            OpenMorgue(uid, component);
        }
    }

    private void OnTrayActivate(EntityUid uid, MorgueTrayComponent component, ActivateInWorldEvent args)
    {
        if (!TryComp<MorgueEntityStorageComponent>(component.Morgue, out var morgue))
            return;

        OnActivate(component.Morgue, morgue, args);
    }

    private void OnExamine(EntityUid uid, MorgueEntityStorageComponent component, ExaminedEvent args)
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

    public void OpenMorgue(EntityUid uid, MorgueEntityStorageComponent? component = null, EntityStorageComponent? storage = null)
    {
        if (!Resolve(uid, ref component, ref storage))
            return;

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

        storage.Open = true;
        var containedArr = storage.Contents.ContainedEntities.ToArray();
        foreach (var contained in containedArr)
        {
            if (storage.Contents.Remove(contained))
            {
                Transform(contained).WorldPosition = Transform(component.Tray).WorldPosition;
                if (TryComp(contained, out IPhysBody? physics))
                    physics.CanCollide = true;
            }
        }
        _entityStorage.ModifyComponents(uid, storage);
        SoundSystem.Play(storage.OpenSound.GetSound(), Filter.Pvs(uid), uid);
    }

    public void CloseMorgue(EntityUid uid, MorgueEntityStorageComponent? component = null, EntityStorageComponent? storage = null)
    {
        if (!Resolve(uid, ref component, ref storage))
            return;

        storage.Open = false;

        var count = 0;
        foreach (var ent in DetermineCollidingEntity(component.Tray))
        {
            if (_container.IsEntityInContainer(ent))
                continue;

            if (!_entityStorage.CanFit(ent, uid))
                continue;

            if (!AddToContents(ent, uid, storage))
                continue;

            count++;
            if (count >= storage.StorageCapacityMax)
                break;
        }

        _entityStorage.ModifyComponents(uid, storage);
        SoundSystem.Play(storage.CloseSound.GetSound(), Filter.Pvs(uid), uid);
        storage.LastInternalOpenAttempt = default;

        if (TryComp<AppearanceComponent>(uid, out var app))
            app.SetData(MorgueVisuals.Open, false);

        CheckContents(uid, component, storage);

        component.TrayContainer.Insert(component.Tray, EntityManager);
    }

    private IEnumerable<EntityUid> DetermineCollidingEntity(EntityUid tray)
    {
        foreach (var entity in _lookup.GetEntitiesIntersecting(tray, flags: LookupFlags.None))
        {
            yield return entity;
        }
    }

    private bool AddToContents(EntityUid entity, EntityUid container, EntityStorageComponent component)
    {
        if (HasComp<SharedBodyComponent>(entity) && !_stando.IsDown(entity))
            return false;
        
        return _entityStorage.AddToContents(entity, container, component);
    }

    private void CheckContents(EntityUid uid, MorgueEntityStorageComponent? morgue = null, EntityStorageComponent? storage = null)
    {
        if (!Resolve(uid, ref morgue, ref storage))
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

    public bool CanOpen(EntityUid user, EntityUid uid, MorgueEntityStorageComponent? component = null, bool silent = false)
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

        return _entityStorage.CanOpen(user, uid, silent);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        foreach (var comp in EntityQuery<MorgueEntityStorageComponent>())
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
