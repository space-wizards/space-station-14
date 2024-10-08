using Content.Server.Radiation.Components;
using Content.Shared.Damage.Components;
using Robust.Shared.Prototypes;

namespace Content.Server.Radiation.EntitySystems;

public sealed class RadiationProtectionSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RadiationProtectionComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<RadiationProtectionComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnInit(EntityUid uid, RadiationProtectionComponent component, ComponentInit args)
    {
        if (!_prototypeManager.TryIndex(component.RadiationProtectionModifierSetId, out var modifier))
            return;
        var buffComp = EnsureComp<DamageProtectionBuffComponent>(uid);
        // add the damage modifier if it isn't in the dict yet
        if (!buffComp.Modifiers.ContainsKey(component.RadiationProtectionModifierSetId))
            buffComp.Modifiers.Add(component.RadiationProtectionModifierSetId, modifier);
    }

    private void OnShutdown(EntityUid uid, RadiationProtectionComponent component, ComponentShutdown args)
    {
        if (!TryComp<DamageProtectionBuffComponent>(uid, out var buffComp))
            return;
        // remove the damage modifier from the dict
        buffComp.Modifiers.Remove(component.RadiationProtectionModifierSetId);
        // if the dict is empty now, remove the buff component
        if (buffComp.Modifiers.Count == 0)
            RemComp<DamageProtectionBuffComponent>(uid);
    }
}
