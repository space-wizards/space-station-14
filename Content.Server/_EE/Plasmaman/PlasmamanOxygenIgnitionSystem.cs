using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Cloning.Components;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared.Inventory;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Plasmaman;
using Content.Shared.Tag;
using Robust.Shared.Prototypes;

namespace Content.Server.Plasmaman;

public sealed partial class PlasmamanOxygenIgnitionSystem : EntitySystem
{
    private const float UpdateDelay = 1f;

    private static readonly ProtoId<TagPrototype> HelmetEvaTag = "HelmetEVA";
    private static readonly ProtoId<TagPrototype> HardsuitTag = "Hardsuit";

    [Dependency] private AtmosphereSystem _atmosphere = default!;
    [Dependency] private FlammableSystem _flammable = default!;
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private TagSystem _tag = default!;

    private float _updateTimer;

    public override void Initialize()
    {
        base.Initialize();
        UpdatesAfter.Add(typeof(AtmosphereSystem));
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        _updateTimer += frameTime;
        if (_updateTimer < UpdateDelay)
            return;

        _updateTimer -= UpdateDelay;

        var query = EntityQueryEnumerator<PlasmamanOxygenIgnitionComponent, MobStateComponent, FlammableComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var ignition, out var mobState, out var flammable, out var transform))
        {
            if (ignition.FireStacksPerUpdate <= 0 ||
                mobState.CurrentState == MobState.Dead ||
                HasComp<BeingClonedComponent>(uid))
                continue;

            var mixture = _atmosphere.GetContainingMixture((uid, transform), excite: true);
            if (mixture == null || mixture.GetMoles(ignition.Gas) < ignition.MolesToIgnite)
                continue;

            if (HasOxygenProtection(uid))
                continue;

            _flammable.AdjustFireStacks(uid, ignition.FireStacksPerUpdate, flammable);
            _flammable.Ignite(uid, uid, flammable);
        }
    }

    private bool HasOxygenProtection(EntityUid uid)
    {
        return HasHeadProtection(uid) && HasBodyProtection(uid);
    }

    private bool HasHeadProtection(EntityUid uid)
    {
        if (!_inventory.TryGetSlotEntity(uid, "head", out var head))
            return false;

        if (TryComp<PlasmamanOxygenProtectionComponent>(head, out var protection) && protection.ProtectsHead)
            return true;

        if (_tag.HasTag(head.Value, HelmetEvaTag))
            return true;

        // Hardsuit helmets and similar sealed headwear have a built-in BreathTool (BreathMask).
        return HasComp<BreathToolComponent>(head);
    }

    private bool HasBodyProtection(EntityUid uid)
    {
        if (_inventory.TryGetSlotEntity(uid, "outerClothing", out var outer))
        {
            if (TryComp<PlasmamanOxygenProtectionComponent>(outer, out var outerProtection) && outerProtection.ProtectsBody)
                return true;

            if (_tag.HasTag(outer.Value, HardsuitTag))
                return true;

            // Any sealed EVA-class outerwear (hardsuits, atmos hardsuit, hazard suits) carries pressure protection.
            if (HasComp<PressureProtectionComponent>(outer))
                return true;
        }

        if (_inventory.TryGetSlotEntity(uid, "jumpsuit", out var jumpsuit) &&
            TryComp<PlasmamanOxygenProtectionComponent>(jumpsuit, out var jumpProtection) &&
            jumpProtection.ProtectsBody)
        {
            return true;
        }

        return false;
    }
}
