using System.Linq;
using Content.Server.Emp;
using Content.Server.IdentityManagement;
using Content.Shared.Clothing.Components;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Emp;
using Content.Shared.IdentityManagement.Components;
using Content.Shared.Inventory;
using Content.Shared.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Clothing.Systems;

public sealed class ChameleonClothingSystem : SharedChameleonClothingSystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IdentitySystem _identity = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ChameleonClothingComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ChameleonClothingComponent, ChameleonPrototypeSelectedMessage>(OnSelected);

        SubscribeLocalEvent<ChameleonClothingComponent, EmpPulseEvent>(OnEmpPulse);
    }

    private void OnMapInit(EntityUid uid, ChameleonClothingComponent component, MapInitEvent args)
    {
        SetSelectedPrototype(uid, component.Default, true, component);
    }

    private void OnSelected(EntityUid uid, ChameleonClothingComponent component, ChameleonPrototypeSelectedMessage args)
    {
        SetSelectedPrototype(uid, args.SelectedId, component: component);
    }

    private void OnEmpPulse(EntityUid uid, ChameleonClothingComponent component, ref EmpPulseEvent args)
    {
        if (!component.AffectedByEmp)
            return;

        if (component.EmpContinuous)
            component.NextEmpChange = _timing.CurTime + TimeSpan.FromSeconds(1f / component.EmpChangeIntensity);

        var pick = GetRandomValidPrototype(component.Slot, component.RequireTag);
        SetSelectedPrototype(uid, pick, component: component);

        args.Affected = true;
        args.Disabled = true;
    }

    private void UpdateUi(EntityUid uid, ChameleonClothingComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        var state = new ChameleonBoundUserInterfaceState(component.Slot, component.Default, component.RequireTag);
        UI.SetUiState(uid, ChameleonUiKey.Key, state);
    }

    /// <summary>
    ///     Change chameleon items name, description and sprite to mimic other entity prototype.
    /// </summary>
    public void SetSelectedPrototype(EntityUid uid, string? protoId, bool forceUpdate = false,
        ChameleonClothingComponent? component = null)
    {
        if (!Resolve(uid, ref component, false))
            return;

        // check that wasn't already selected
        // forceUpdate on component init ignores this check
        if (component.Default == protoId && !forceUpdate)
            return;

        // make sure that it is valid change
        if (string.IsNullOrEmpty(protoId) || !_proto.TryIndex(protoId, out EntityPrototype? proto))
            return;
        if (!IsValidTarget(proto, component.Slot, component.RequireTag))
            return;
        component.Default = protoId;

        UpdateIdentityBlocker(uid, component, proto);
        UpdateVisuals(uid, component);
        UpdateUi(uid, component);
        Dirty(uid, component);
    }

    /// <summary>
    ///     Get a random prototype for a given slot.
    /// </summary>
    public string GetRandomValidPrototype(SlotFlags slot, string? tag = null)
    {
        return _random.Pick(GetValidTargets(slot, tag).ToList());
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        // Randomize EMP-affected clothing
        var query = EntityQueryEnumerator<EmpDisabledComponent, ChameleonClothingComponent>();
        while (query.MoveNext(out var uid, out _, out var chameleon))
        {
            if (!chameleon.EmpContinuous)
                continue;

            if (_timing.CurTime < chameleon.NextEmpChange)
                continue;

            // randomly pick cloth element from available and apply it
            var pick = GetRandomValidPrototype(chameleon.Slot, chameleon.RequireTag);
            SetSelectedPrototype(uid, pick, component: chameleon);

            chameleon.NextEmpChange += TimeSpan.FromSeconds(1f / chameleon.EmpChangeIntensity);
        }
    }

    private void UpdateIdentityBlocker(EntityUid uid, ChameleonClothingComponent component, EntityPrototype proto)
    {
        if (proto.HasComponent<IdentityBlockerComponent>(Factory))
            EnsureComp<IdentityBlockerComponent>(uid);
        else
            RemComp<IdentityBlockerComponent>(uid);

        if (component.User != null)
            _identity.QueueIdentityUpdate(component.User.Value);
    }
}
