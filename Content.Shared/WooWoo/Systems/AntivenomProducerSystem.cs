using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Content.Shared.WooWoo.Components.Antivenom;
using Robust.Shared.Prototypes;

namespace Content.Shared.WooWoo.Systems.Antivenom;

public abstract class SharedAntivenomProducerSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<AntivenomProducerComponent, ComponentInit>(OnAntivenomProducerInit);
    }

    public void OnAntivenomProducerInit(Entity<AntivenomProducerComponent> entity, ref ComponentInit args)
    {
        CacheVenomConfigs(entity);
    }

    /// <summary>
    /// setup lookup caches
    /// </summary>
    public void CacheVenomConfigs(Entity<AntivenomProducerComponent> entity)
    {
        entity.Comp.ConfigByVenom.Clear();
        entity.Comp.ConfigByAntivenom.Clear();

        foreach (var cfg in entity.Comp.ImmunityConfigs)
        {
            entity.Comp.ConfigByVenom[cfg.Venom] = cfg;
            entity.Comp.ConfigByAntivenom[cfg.Antivenom] = cfg;
        }
    }

    /// <summary>
    /// Track a metabolized venom and its immune stage
    /// </summary>
    public void AccumulateImmunity(EntityUid ent, AntivenomProducerComponent comp, ReagentId reagent, FixedPoint2 metabolizedAmount)
    {
        if (comp.ImmunoCompromised || metabolizedAmount <= FixedPoint2.Zero)
            return;

        if (comp.ConfigByVenom.Count == 0)
            return;

        if (!comp.ConfigByVenom.TryGetValue(reagent.Prototype, out var cfg))
            return; // not a tracked venom

        // accumulate
        if (!comp.MetabolizedTotals.TryGetValue(reagent.Prototype, out var cur))
            cur = FixedPoint2.Zero;

        var next = cur + metabolizedAmount;
        comp.MetabolizedTotals[reagent.Prototype] = next;

        // update stage
        var newStage = ComputeStage(ent, next, cfg);
        if (!comp.UnlockedImmunities.TryGetValue(reagent.Prototype, out var oldStage))

            if (newStage != oldStage)
            {
                // consider whether to increase number of metab'ed reagents so we cant get chock a block with antivenoms.
                // Its currently in server though, and I'm not sure its necessary.
                if (newStage == 0u)
                    comp.UnlockedImmunities.Remove(reagent.Prototype);
                else
                    comp.UnlockedImmunities[reagent.Prototype] = newStage;
            }
    }

    /// <summary>
    /// Computes an immune stage based on accumulated metabolized reagents
    /// </summary>
    public uint ComputeStage(EntityUid ent, FixedPoint2 metabolized, AntivenomImmunityConfig cfg)
    {
        if (cfg.Threshold <= FixedPoint2.Zero)
        {
            Log.Warning($"{ToPrettyString(ent)} has a venom immunity threshold of negative or zero");
            return 0; // you done fucked up son.
        }

        var stage = (uint)Math.Floor(metabolized.Float() / cfg.Threshold.Float());
        return stage;
    }

    /// <summary>
    /// returns true if immune stage >= 1, false otherwise
    /// </summary>
    public bool TryGetImmuneStage(EntityUid ent, ReagentId reagentId, out uint stage)
    {
        if (TryGetImmuneStage(ent, reagentId.Prototype, out stage))
            return true;
        return false;
    }

    /// <summary>
    /// returns true if immune stage >= 1, false otherwise
    /// </summary>
    public bool TryGetImmuneStage(EntityUid ent, string reagentPrototype, out uint stage)
    {
        stage = 0;
        if (!TryComp<AntivenomProducerComponent>(ent, out var comp))
            return false;
        if (!comp.UnlockedImmunities.TryGetValue(reagentPrototype, out stage))
            return false;
        return true;
    }

    /// <summary>
    /// Handle creating antivenom reagents
    /// </summary>
    public void ProduceAntivenom(EntityUid ent, AntivenomProducerComponent comp, Solution solution)
    {
        if (comp.ImmunoCompromised || comp.UnlockedImmunities.Count == 0)
            return;

        foreach (var (venom, stage) in comp.UnlockedImmunities)
        {
            if (!comp.ConfigByVenom.TryGetValue(venom, out var cfg))
                continue;

            var amount = cfg.AVPerStage * stage;
            if (amount <= FixedPoint2.Zero)
                continue;

            solution.AddReagent(cfg.Antivenom, amount);
        }
    }

    // Parting Thoughts:
    // put antivenom in bloodstream & also ensure there is room in the bloodstream to add it somehow up to some quantity.
    // also put some in the chemstream to get passive effect from it. This is a dirty hack to account for bloodstream being separaate from chemstream metab. Kill it later.
    // ^ I think syringes solved this already (well its dumb but the curse is delt with there), I can just add it to the chemstream and then when bloodstream is unified we can move it to that.
}
