using Content.Shared.Clothing.Components;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Emp;
using Content.Shared.IdentityManagement;
using Content.Shared.IdentityManagement.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server.Clothing.Systems;

public sealed partial class ChameleonClothingSystem : SharedChameleonClothingSystem
{
    private static readonly EntityTimerId EmpChangeTimer = new("emp-change");

    [Dependency] private IdentitySystem _identity = default!;
    [Dependency] private EntityTimerSystem _timers = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ChameleonClothingComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ChameleonClothingComponent, ChameleonPrototypeSelectedMessage>(OnSelected);
        SubscribeLocalEvent<EmpDisabledComponent, ComponentStartup>(OnEmpStartup);
        SubscribeLocalEvent<ChameleonClothingComponent, EntityTimerEvent>(OnTimer);
    }

    private void OnMapInit(EntityUid uid, ChameleonClothingComponent component, MapInitEvent args)
    {
        SetSelectedPrototype(uid, component.Default, true, component: component);
    }

    private void OnSelected(EntityUid uid, ChameleonClothingComponent component, ChameleonPrototypeSelectedMessage args)
    {
        SetSelectedPrototype(uid, args.SelectedId, component: component);
    }

    private void UpdateUi(EntityUid uid, ChameleonClothingComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        var state = new ChameleonBoundUserInterfaceState(component.Slot, component.Default, component.RequireTag);
        UI.SetUiState(uid, ChameleonUiKey.Key, state);
    }

    public override void SetSelectedPrototype(EntityUid uid, string? protoId, bool forceUpdate = false, bool validate = true,
        ChameleonClothingComponent? component = null)
    {
        if (!Resolve(uid, ref component, false))
            return;

        // check that wasn't already selected
        // forceUpdate on component init ignores this check
        if (component.Default == protoId && !forceUpdate)
            return;

        // make sure that it is valid change
        if (string.IsNullOrEmpty(protoId) || !ProtoMan.TryIndex(protoId, out EntityPrototype? proto))
            return;

        if (validate && !IsValidTarget(proto, component.Slot, component.RequireTag))
            return;

        component.Default = protoId;

        UpdateIdentityBlocker(uid, component, proto);
        UpdateVisuals(uid, component);
        UpdateUi(uid, component);
        Dirty(uid, component);
    }

    private void OnEmpStartup(Entity<EmpDisabledComponent> ent, ref ComponentStartup args)
    {
        if (TryComp<ChameleonClothingComponent>(ent, out var chameleon) && chameleon.EmpContinuous)
            _timers.SetTimerAt<ChameleonClothingComponent>((ent.Owner, chameleon), EmpChangeTimer, Timing.CurTime);
    }

    private void OnTimer(Entity<ChameleonClothingComponent> ent, ref EntityTimerEvent args)
    {
        if (args.Id != EmpChangeTimer || !ent.Comp.EmpContinuous || !HasComp<EmpDisabledComponent>(ent))
            return;

        var pick = GetRandomValidPrototype(ent.Comp.Slot, ent.Comp.RequireTag);
        SetSelectedPrototype(ent, pick, component: ent.Comp);
        ent.Comp.NextEmpChange = args.ScheduledTime + TimeSpan.FromSeconds(1f / ent.Comp.EmpChangeIntensity);
        _timers.SetTimerAt(ent, EmpChangeTimer, ent.Comp.NextEmpChange);
    }

    private void UpdateIdentityBlocker(EntityUid uid, ChameleonClothingComponent component, EntityPrototype proto)
    {
        if (proto.HasComp<IdentityBlockerComponent>(Factory))
            EnsureComp<IdentityBlockerComponent>(uid);
        else
            RemComp<IdentityBlockerComponent>(uid);

        if (component.User != null)
            _identity.QueueIdentityUpdate(component.User.Value);
    }
}
