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
        if (!proto.TryGetComponent(out PointLightComponent? pointLight, _factory)) RemComp<PointLightComponent>(uid);
        else
        {
            if (HasComp<PointLightComponent>(uid))
                RemComp<PointLightComponent>(uid);

            AddComp(uid, pointLight);
            Dirty(uid, pointLight);
        }

        if (!proto.TryGetComponent(out LightBehaviourComponent? lightBehaviour, _factory)) RemComp<LightBehaviourComponent>(uid);
        else
        {
            if (HasComp<LightBehaviourComponent>(uid))
                RemComp<LightBehaviourComponent>(uid);

            AddComp(uid, lightBehaviour);
        }

        if (!proto.TryGetComponent(out HandheldLightComponent? handheldLight, _factory)) RemComp<HandheldLightComponent>(uid);
        else
        {
            if (HasComp<HandheldLightComponent>(uid))
                RemComp<HandheldLightComponent>(uid);

            AddComp(uid, handheldLight);
            Dirty(uid, handheldLight);
        }

        if (!proto.TryGetComponent(out BatteryComponent? battery, _factory)) RemComp<BatteryComponent>(uid);
        else
        {
            if (HasComp<BatteryComponent>(uid))
                RemComp<BatteryComponent>(uid);

            AddComp(uid, battery);
        }

        if (!proto.TryGetComponent(out AutoRechargeComponent? autoRecharge, _factory)) RemComp<AutoRechargeComponent>(uid);
        else
        {
            if (HasComp<AutoRechargeComponent>(uid))
                RemComp<AutoRechargeComponent>(uid);

            AddComp(uid, autoRecharge);
        }
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
}
