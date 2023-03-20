using Content.Server.Medical.Components;
using Content.Shared.DoAfter;
using Content.Shared.Body.Components;
using Content.Shared.DragDrop;
using Content.Shared.Emag.Systems;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Standing;
using Content.Shared.Stunnable;
using Content.Shared.Verbs;
using Robust.Shared.Containers;
using Robust.Shared.Player;
using Content.Shared.ActionBlocker;

namespace Content.Shared.Medical.Cryogenics;

public abstract partial class SharedCryoPodSystem: EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearanceSystem = default!;
    [Dependency] private readonly StandingStateSystem _standingStateSystem = default!;
    [Dependency] private readonly ActionBlockerSystem _blocker = default!;
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        InitializeInsideCryoPod();
    }
    protected void OnCryoPodCanDropOn(EntityUid uid, SharedCryoPodComponent component, ref CanDropTargetEvent args)
    {
        args.CanDrop = args.CanDrop && CanCryoPodInsert(uid, args.Dragged, component);
        args.Handled = true;
    }
    protected void OnComponentInit(EntityUid uid, SharedCryoPodComponent cryoPodComponent, ComponentInit args)
    {
        base.Initialize();
        cryoPodComponent.BodyContainer = _containerSystem.EnsureContainer<ContainerSlot>(uid, $"scanner-body");
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
        if (IsOccupied(cryoPodComponent))
        {
            AlternativeVerb verb = new();
            verb.Act = () => EjectBody(uid, cryoPodComponent);
            verb.Category = VerbCategory.Eject;
            verb.Text = Loc.GetString("cryo-pod-verb-noun-occupant");
            verb.Priority = 1; // Promote to top to make ejecting the ALT-click action
            args.Verbs.Add(verb);
        }

        // Self-insert verb
        if (!IsOccupied(cryoPodComponent) &&
            CanCryoPodInsert(uid, args.User, cryoPodComponent) &&
            _blocker.CanMove(args.User))
        {
            AlternativeVerb verb = new();
            verb.Act = () => InsertBody(uid, args.User, cryoPodComponent);
            verb.Text = Loc.GetString("cryo-pod-verb-noun-occupant");
            args.Verbs.Add(verb);
        }
    }

    protected void AddInsertOtherVerb(EntityUid uid, SharedCryoPodComponent cryoPodComponent, GetVerbsEvent<InteractionVerb> args)
    {
        if (args.Using == null ||
                !args.CanAccess ||
                !args.CanInteract ||
                IsOccupied(cryoPodComponent) ||
                !CanCryoPodInsert(uid, args.Using.Value, cryoPodComponent))
            return;

        string name = "Unknown";
        if (TryComp<MetaDataComponent>(args.Using.Value, out var metadata))
            name = metadata.EntityName;

        InteractionVerb verb = new()
        {
            Act = () => InsertBody(uid, args.Using.Value, cryoPodComponent),
            Category = VerbCategory.Insert,
            Text = name
        };
        args.Verbs.Add(verb);
    }
    protected void OnEmagged(EntityUid uid, SharedCryoPodComponent? cryoPodComponent, ref GotEmaggedEvent args)
    {
        if (!Resolve(uid, ref cryoPodComponent))
        {
            return;
        }

        cryoPodComponent.PermaLocked = true;
        cryoPodComponent.Locked = true;
        args.Handled = true;
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
    public bool IsOccupied(SharedCryoPodComponent scannerComponent)
    {
        return scannerComponent.BodyContainer.ContainedEntity != null;
    }
    public bool CanCryoPodInsert(EntityUid uid, EntityUid target, SharedCryoPodComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return false;

        return HasComp<BodyComponent>(target);
    }
    #region Event records

    protected record CryoPodPryFinished;
    protected record CryoPodPryInterrupted;

    #endregion
}
