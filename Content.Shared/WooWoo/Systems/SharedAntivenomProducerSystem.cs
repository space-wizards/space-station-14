using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Content.Shared.WooWoo.Components.Antivenom;
using Robust.Shared.Prototypes;

namespace Content.Shared.WooWoo.Systems.Antivenom;

public abstract class SharedAntivenomProducerSystem : EntitySystem
{
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
        // check if we have a non-null stage, if not treat it as zero temporarily so immunities can be removed fail-safe by setting them to zero
        var oldStage = comp.UnlockedImmunities.TryGetValue(reagent.Prototype, out var s) ? s : 0u;

        if (newStage != oldStage)
        {
            if (newStage == 0u)
                comp.UnlockedImmunities.Remove(reagent.Prototype);
            else
                comp.UnlockedImmunities[reagent.Prototype] = newStage;
        }

        // gotta do this since metab stuff is all server sided. I thought better to put it here than in the metab system.
        Dirty(ent, comp);
    }

    /// <summary>
    /// Computes an immune stage based on accumulated metabolized reagents and entity specific antivenom config
    /// </summary>
    public uint ComputeStage(EntityUid ent, FixedPoint2 metabolized, AntivenomImmunityConfig cfg)
    {
        if (cfg.Threshold <= FixedPoint2.Zero)
        {
            Log.Warning($"{ToPrettyString(ent)} has a venom immunity threshold of negative or zero");
            return 0; // you done fucked up son.
        }

        var stage = (uint)Math.Min(cfg.MaxStage, Math.Floor(metabolized.Float() / cfg.Threshold.Float()));
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

    // always create reagents on server
    public abstract void CreateAntivenom(
        Entity<SolutionComponent> soln,
        AntivenomProducerComponent comp
        );

    // Parting Thoughts:
    // put antivenom in bloodstream & also ensure there is room in the bloodstream to add it somehow up to some quantity.
    // also put some in the chemstream to get passive effect from it. This is a dirty hack to account for bloodstream being separaate from chemstream metab. Kill it later.
    // ^ I think syringes solved this already (well its dumb but the curse is delt with there), I can just add it to the chemstream and then when bloodstream is unified we can move it to that.
}
