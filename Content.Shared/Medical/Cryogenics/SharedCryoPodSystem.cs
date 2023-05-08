using Content.Server.Medical.Components;
using Content.Shared.Body.Components;
using Content.Shared.DoAfter;
using Content.Shared.DragDrop;
using Content.Shared.Emag.Systems;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Standing;
using Content.Shared.Stunnable;
using Content.Shared.Verbs;
using Robust.Shared.Containers;
using Robust.Shared.Serialization;

namespace Content.Shared.Medical.Cryogenics;

public abstract partial class SharedCryoPodSystem: EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearanceSystem = default!;
    [Dependency] private readonly StandingStateSystem _standingStateSystem = default!;
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CryoPodComponent, CanDropTargetEvent>(OnCryoPodCanDropOn);
        InitializeInsideCryoPod();
    }

    private void OnCryoPodCanDropOn(EntityUid uid, CryoPodComponent component, ref CanDropTargetEvent args)
    {
        if (args.Handled)
            return;

        args.CanDrop = HasComp<BodyComponent>(args.Dragged);
        args.Handled = true;
    }

    protected void OnComponentInit(EntityUid uid, CryoPodComponent cryoPodComponent, ComponentInit args)
    {
        cryoPodComponent.BodyContainer = _containerSystem.EnsureContainer<ContainerSlot>(uid, "scanner-body");
    }

    protected void UpdateAppearance(EntityUid uid, CryoPodComponent? cryoPod = null, AppearanceComponent? appearance = null)
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
        _appearanceSystem.SetData(uid, CryoPodComponent.CryoPodVisuals.ContainsEntity, cryoPod.BodyContainer.ContainedEntity == null, appearance);
        _appearanceSystem.SetData(uid, CryoPodComponent.CryoPodVisuals.IsOn, cryoPodEnabled, appearance);
    }

    public void InsertBody(EntityUid uid, EntityUid target, CryoPodComponent cryoPodComponent)
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

    public void TryEjectBody(EntityUid uid, EntityUid userId, CryoPodComponent? cryoPodComponent)
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

    public virtual void EjectBody(EntityUid uid, CryoPodComponent? cryoPodComponent)
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

    protected void AddAlternativeVerbs(EntityUid uid, CryoPodComponent cryoPodComponent, GetVerbsEvent<AlternativeVerb> args)
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

    protected void OnEmagged(EntityUid uid, CryoPodComponent? cryoPodComponent, ref GotEmaggedEvent args)
    {
        if (!Resolve(uid, ref cryoPodComponent))
        {
            return;
        }

        cryoPodComponent.PermaLocked = true;
        cryoPodComponent.Locked = true;
        args.Handled = true;
    }

    protected void OnCryoPodPryFinished(EntityUid uid, CryoPodComponent cryoPodComponent, CryoPodPryFinished args)
    {
        if (args.Cancelled)
            return;

        EjectBody(uid, cryoPodComponent);
    }

    [Serializable, NetSerializable]
    public sealed class CryoPodPryFinished : SimpleDoAfterEvent
    {
    }

    [Serializable, NetSerializable]
    public sealed class CryoPodDragFinished : SimpleDoAfterEvent
    {
    }
}
