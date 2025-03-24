using Content.Server.Charges.Components;
using Content.Server.IdentityManagement;
using Content.Server.Light.Components;
using Content.Server.Power.Components;
using Content.Shared.Clothing.Components;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.IdentityManagement.Components;
using Content.Shared.Light.Components;
using Content.Shared.Prototypes;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Server.Clothing.Systems;

public sealed class ChameleonClothingSystem : SharedChameleonClothingSystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IComponentFactory _factory = default!;
    [Dependency] private readonly IdentitySystem _identity = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ChameleonClothingComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ChameleonClothingComponent, ChameleonPrototypeSelectedMessage>(OnSelected);
    }

    private void OnMapInit(EntityUid uid, ChameleonClothingComponent component, MapInitEvent args)
    {
        SetSelectedPrototype(uid, component.Default, true, component);
    }

    private void OnSelected(EntityUid uid, ChameleonClothingComponent component, ChameleonPrototypeSelectedMessage args)
    {
        SetSelectedPrototype(uid, args.SelectedId, component: component);
    }

    private void UpdateUi(EntityUid uid, ChameleonClothingComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        var state = new ChameleonBoundUserInterfaceState(component.Slot, component.Default, component.RequireTags);
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
        if (!IsValidTarget(proto, component.Slot, component.RequireTags))
            return;
        component.Default = protoId;

        UpdateIdentityBlocker(uid, component, proto);
        UpdateHelmetLights(uid, component, proto);
        UpdateToggleableChameleonComponent(uid, proto);
        UpdateVisuals(uid, component);
        UpdateUi(uid, component);
        Dirty(uid, component);
    }


    private void UpdateIdentityBlocker(EntityUid uid, ChameleonClothingComponent component, EntityPrototype proto)
    {
        if (proto.HasComponent<IdentityBlockerComponent>(_factory))
            EnsureComp<IdentityBlockerComponent>(uid);
        else
            RemComp<IdentityBlockerComponent>(uid);

        if (component.User != null)
            _identity.QueueIdentityUpdate(component.User.Value);
    }


    private void UpdateHelmetLights(EntityUid uid, ChameleonClothingComponent component, EntityPrototype proto)
    {
        RemoveAndAddComponent<PointLightComponent>(uid, proto, true);
        RemoveAndAddComponent<LightBehaviourComponent>(uid, proto, false);
        RemoveAndAddComponent<BatteryComponent>(uid, proto, false);
        RemoveAndAddComponent<BatterySelfRechargerComponent>(uid, proto, false);
        RemoveAndAddComponent<AutoRechargeComponent>(uid, proto, false);
    }

    private void UpdateToggleableChameleonComponent(EntityUid uid, EntityPrototype proto)
    {
        if (TryComp(uid, out ToggleableClothingComponent? toggleableClothingComponent) && TryComp(toggleableClothingComponent?.ClothingUid, out ChameleonClothingComponent? toggleableChameleonClothingComponent))
        {
            proto.TryGetComponent(out ToggleableClothingComponent? newToggleableClothingComponent, _factory);

            if (newToggleableClothingComponent != null) {
                SetSelectedPrototype((EntityUid)toggleableClothingComponent.ClothingUid!, newToggleableClothingComponent?.ClothingPrototype);
                return;
            }

            proto.TryGetComponent(out ChameleonAttachedHelmetComponent? chameleonAttachedHelmetComponent, _factory);

            if (chameleonAttachedHelmetComponent != null) {
                SetSelectedPrototype((EntityUid)toggleableClothingComponent.ClothingUid!, chameleonAttachedHelmetComponent?.ClothingPrototype);
                return;
            }
        }
    }

    private void RemoveAndAddComponent<T>(EntityUid uid, EntityPrototype proto, bool dirty) where T : IComponent, new()
    {
        RemComp<T>(uid);
        if (!proto.TryGetComponent(out T? component, _factory))
            return;

        AddComp(uid, component);

        if (dirty)
            Dirty(uid, component);
    }
}
