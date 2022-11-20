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

        SubscribeLocalEvent<SharedCryoPodComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<SharedCryoPodComponent, DestructionEventArgs>(OnDestroyed);
        SubscribeLocalEvent<SharedCryoPodComponent, GetVerbsEvent<AlternativeVerb>>(AddAlternativeVerbs);
        SubscribeLocalEvent<SharedCryoPodComponent, GotEmaggedEvent>(OnEmagged);
        SubscribeLocalEvent<SharedCryoPodComponent, DoInsertCryoPodEvent>(DoInsertCryoPod);
        SubscribeLocalEvent<SharedCryoPodComponent, CryoPodPryFinished>(OnCryoPodPryFinished);
        SubscribeLocalEvent<SharedCryoPodComponent, CryoPodPryInterrupted>(OnCryoPodPryInterrupted);

        InitializeInsideCryoPod();
    }

    private void OnComponentInit(EntityUid uid, SharedCryoPodComponent cryoPodComponent, ComponentInit args)
    {
        cryoPodComponent.BodyContainer = _containerSystem.EnsureContainer<ContainerSlot>(uid, "scanner-body");
    }

    private void OnDestroyed(EntityUid uid, SharedCryoPodComponent? cryoPodComponent, DestructionEventArgs args)
    {
        if (!Resolve(uid, ref cryoPodComponent))
        {
            return;
        }
        EjectBody(uid, cryoPodComponent);
    }

    protected void UpdateAppearance(EntityUid uid, SharedCryoPodComponent? cryoPod = null)
    {
        if (!Resolve(uid, ref cryoPod))
            return;

        var cryoPodEnabled = HasComp<ActiveCryoPodComponent>(uid);
        _appearanceSystem.SetData(uid, SharedCryoPodComponent.CryoPodVisuals.ContainsEntity, cryoPod.BodyContainer.ContainedEntity == null);
        _appearanceSystem.SetData(uid, SharedCryoPodComponent.CryoPodVisuals.IsOn, cryoPodEnabled);
        if (TryComp<SharedPointLightComponent>(uid, out var light))
        {
            light.Enabled = cryoPodEnabled && cryoPod.BodyContainer.ContainedEntity != null;
        }

        _appearanceSystem.SetData(uid,SharedCryoPodComponent.CryoPodVisuals.PanelOpen, false);
    }

    public void InsertBody(EntityUid uid, EntityUid target, SharedCryoPodComponent cryoPodComponent)
    {
        if (cryoPodComponent.BodyContainer.ContainedEntity != null)
            return;

        if (!HasComp<MobStateComponent>(target))
            return;

        var xform = Transform(target);
        cryoPodComponent.BodyContainer.Insert(target, transform: xform);

        var comp = EnsureComp<InsideCryoPodComponent>(target);
        comp.Holder = cryoPodComponent.Owner;
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
            _popupSystem.PopupEntity(Loc.GetString("cryo-pod-locked"), uid, Filter.Entities(userId));
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
        RemComp<InsideCryoPodComponent>(contained);

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

    private void AddAlternativeVerbs(EntityUid uid, SharedCryoPodComponent cryoPodComponent, GetVerbsEvent<AlternativeVerb> args)
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

    private void OnEmagged(EntityUid uid, SharedCryoPodComponent? cryoPodComponent, GotEmaggedEvent args)
    {
        if (!Resolve(uid, ref cryoPodComponent))
        {
            return;
        }

        cryoPodComponent.PermaLocked = true;
        cryoPodComponent.Locked = true;
        args.Handled = true;
    }

    private void DoInsertCryoPod(EntityUid uid, SharedCryoPodComponent cryoPodComponent, DoInsertCryoPodEvent args)
    {
        InsertBody(uid, args.ToInsert, cryoPodComponent);
    }

    private void OnCryoPodPryFinished(EntityUid uid, SharedCryoPodComponent cryoPodComponent, CryoPodPryFinished args)
    {
        cryoPodComponent.IsPrying = false;
        EjectBody(uid, cryoPodComponent);
    }

    private void OnCryoPodPryInterrupted(EntityUid uid, SharedCryoPodComponent cryoPodComponent, CryoPodPryInterrupted args)
    {
        cryoPodComponent.IsPrying = false;
    }

    #region Event records

    protected record DoInsertCryoPodEvent(EntityUid ToInsert);
    protected record CryoPodPryFinished;
    protected record CryoPodPryInterrupted;

    #endregion
}
