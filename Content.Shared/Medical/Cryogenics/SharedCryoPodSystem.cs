using Content.Server.Medical.Components;
using Content.Shared.Destructible;
using Content.Shared.Emag.Systems;
using Content.Shared.MobState.Components;
using Content.Shared.MobState.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Standing;
using Content.Shared.Stunnable;
using Content.Shared.Verbs;
using Robust.Shared.Containers;
using Robust.Shared.Player;

namespace Content.Shared.Medical.Cryogenics;

public abstract partial class SharedCryoPodSystem: EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearanceSystem = default!;
    [Dependency] private readonly StandingStateSystem _standingStateSystem = default!;
    [Dependency] private readonly SharedMobStateSystem _mobStateSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        InitializeInsideCryoPod();
    }

    protected void OnComponentInit(EntityUid uid, SharedCryoPodComponent cryoPodComponent, ComponentInit args)
    {
        cryoPodComponent.BodyContainer = _containerSystem.EnsureContainer<ContainerSlot>(uid, "scanner-body");
    }

    protected void UpdateAppearance(EntityUid uid, SharedCryoPodComponent? cryoPod = null, AppearanceComponent? appearance = null)
    {
        if (!Resolve(uid, ref cryoPod))
            return;
        var cryoPodEnabled = HasComp<ActiveCryoPodComponent>(uid);
        if (TryComp<SharedPointLightComponent>(uid, out var light))
        {
            light.Enabled = cryoPodEnabled && cryoPod.BodyContainer.ContainedEntity != null;
        }

        if (!Resolve(uid, ref appearance))
            return;
        _appearanceSystem.SetData(uid, SharedCryoPodComponent.CryoPodVisuals.ContainsEntity, cryoPod.BodyContainer.ContainedEntity == null, appearance);
        _appearanceSystem.SetData(uid, SharedCryoPodComponent.CryoPodVisuals.IsOn, cryoPodEnabled, appearance);
    }

    public void InsertBody(EntityUid uid, EntityUid target, SharedCryoPodComponent cryoPodComponent)
    {
        if (cryoPodComponent.BodyContainer.ContainedEntity != null)
            return;

        if (!HasComp<MobStateComponent>(target))
            return;

        var xform = Transform(target);
        cryoPodComponent.BodyContainer.Insert(target, transform: xform);

        EnsureComp<InsideCryoPodComponent>(target);
        _standingStateSystem.Stand(target, force: true); // Force-stand the mob so that the cryo pod sprite overlays it fully

        UpdateAppearance(uid, cryoPodComponent);
    }

    public void TryEjectBody(EntityUid uid, EntityUid userId, SharedCryoPodComponent? cryoPodComponent)
    {
        if (!Resolve(uid, ref cryoPodComponent))
        {
            return;
        }

        if (cryoPodComponent.Locked)
        {
            _popupSystem.PopupEntity(Loc.GetString("cryo-pod-locked"), uid, userId);
            return;
        }

        EjectBody(uid, cryoPodComponent);
    }

    public virtual void EjectBody(EntityUid uid, SharedCryoPodComponent? cryoPodComponent)
    {
        if (!Resolve(uid, ref cryoPodComponent))
            return;

        if (cryoPodComponent.BodyContainer.ContainedEntity is not {Valid: true} contained)
            return;

        cryoPodComponent.BodyContainer.Remove(contained);
        // InsideCryoPodComponent is removed automatically in its EntGotRemovedFromContainerMessage listener
        // RemComp<InsideCryoPodComponent>(contained);

        // Restore the correct position of the patient. Checking the components manually feels hacky, but I did not find a better way for now.
        if (HasComp<KnockedDownComponent>(contained) || _mobStateSystem.IsIncapacitated(contained))
        {
            _standingStateSystem.Down(contained);
        }
        else
        {
            _standingStateSystem.Stand(contained);
        }

        UpdateAppearance(uid, cryoPodComponent);
    }

    protected void AddAlternativeVerbs(EntityUid uid, SharedCryoPodComponent cryoPodComponent, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        // Eject verb
        if (cryoPodComponent.BodyContainer.ContainedEntity != null)
        {
            args.Verbs.Add(new AlternativeVerb
            {
                Text = Loc.GetString("cryo-pod-verb-noun-occupant"),
                Category = VerbCategory.Eject,
                Priority = 1, // Promote to top to make ejecting the ALT-click action
                Act = () => TryEjectBody(uid, args.User, cryoPodComponent)
            });
        }
    }

    protected void OnEmagged(EntityUid uid, SharedCryoPodComponent? cryoPodComponent, GotEmaggedEvent args)
    {
        if (!Resolve(uid, ref cryoPodComponent))
        {
            return;
        }

        cryoPodComponent.PermaLocked = true;
        cryoPodComponent.Locked = true;
        args.Handled = true;
    }

    protected void DoInsertCryoPod(EntityUid uid, SharedCryoPodComponent cryoPodComponent, DoInsertCryoPodEvent args)
    {
        cryoPodComponent.DragDropCancelToken = null;
        InsertBody(uid, args.ToInsert, cryoPodComponent);
    }

    protected void DoInsertCancelCryoPod(EntityUid uid, SharedCryoPodComponent cryoPodComponent, DoInsertCancelledCryoPodEvent args)
    {
        cryoPodComponent.DragDropCancelToken = null;
    }

    protected void OnCryoPodPryFinished(EntityUid uid, SharedCryoPodComponent cryoPodComponent, CryoPodPryFinished args)
    {
        cryoPodComponent.IsPrying = false;
        EjectBody(uid, cryoPodComponent);
    }

    protected void OnCryoPodPryInterrupted(EntityUid uid, SharedCryoPodComponent cryoPodComponent, CryoPodPryInterrupted args)
    {
        cryoPodComponent.IsPrying = false;
    }

    #region Event records

    protected record DoInsertCryoPodEvent(EntityUid ToInsert);
    protected record DoInsertCancelledCryoPodEvent;
    protected record CryoPodPryFinished;
    protected record CryoPodPryInterrupted;

    #endregion
}
