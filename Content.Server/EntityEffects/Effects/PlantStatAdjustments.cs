using Content.Server.Botany.Components;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.EntityEffects.Effects;

/// <summary>
///     Changes the value on a plant's WaterConsumption value randomly when applied.
/// </summary>
public sealed partial class PlantMutateWaterConsumption : EntityEffect
{
    [DataField]
    public float MinValue = 0.3f;
    [DataField]
    public float MaxValue = 0.9f;

    public override void Effect(EntityEffectBaseArgs args)
    {
        var plantholder = args.EntityManager.GetComponent<PlantHolderComponent>(args.TargetEntity);

        if (plantholder.Seed == null)
            return;

        var random = IoCManager.Resolve<IRobustRandom>();
        plantholder.Seed.WaterConsumption = random.NextFloat(MinValue, MaxValue);
    }

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "TODO";
    }
}

/// <summary>
///     Changes the value on a plant's NutrientConsumption value randomly when applied.
/// </summary>
public sealed partial class PlantMutateNutrientConsumption : EntityEffect
{
    [DataField]
    public float MinValue = 0.05f;
    [DataField]
    public float MaxValue = 1.2f;

    public override void Effect(EntityEffectBaseArgs args)
    {
        var plantholder = args.EntityManager.GetComponent<PlantHolderComponent>(args.TargetEntity);

        if (plantholder.Seed == null)
            return;

        var random = IoCManager.Resolve<IRobustRandom>();
        plantholder.Seed.NutrientConsumption = random.NextFloat(MinValue, MaxValue);
    }

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "TODO";
    }
}

/// <summary>
///     Changes the value on a plant's IdealHeat value randomly when applied.
/// </summary>
public sealed partial class PlantMutateIdealHeat : EntityEffect
{
    [DataField]
    public float MinValue = 263f;
    [DataField]
    public float MaxValue = 323f;

    public override void Effect(EntityEffectBaseArgs args)
    {
        var plantholder = args.EntityManager.GetComponent<PlantHolderComponent>(args.TargetEntity);

        if (plantholder.Seed == null)
            return;

        var random = IoCManager.Resolve<IRobustRandom>();
        plantholder.Seed.IdealHeat = random.NextFloat(MinValue, MaxValue);
    }

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "TODO";
    }
}

/// <summary>
///     Changes the value on a plant's HeatTolerance value randomly when applied.
/// </summary>
public sealed partial class PlantMutateHeatTolerance : EntityEffect
{
    [DataField]
    public float MinValue = 2f;
    [DataField]
    public float MaxValue = 25f;

    public override void Effect(EntityEffectBaseArgs args)
    {
        var plantholder = args.EntityManager.GetComponent<PlantHolderComponent>(args.TargetEntity);

        if (plantholder.Seed == null)
            return;

        var random = IoCManager.Resolve<IRobustRandom>();
        plantholder.Seed.HeatTolerance = random.NextFloat(MinValue, MaxValue);
    }

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "TODO";
    }
}

/// <summary>
///     Changes the value on a plant's IdealLight value randomly when applied.
/// </summary>
public sealed partial class PlantMutateIdealLight : EntityEffect
{
    [DataField]
    public float MinValue = 0f;
    [DataField]
    public float MaxValue = 14f;

    public override void Effect(EntityEffectBaseArgs args)
    {
        var plantholder = args.EntityManager.GetComponent<PlantHolderComponent>(args.TargetEntity);

        if (plantholder.Seed == null)
            return;

        var random = IoCManager.Resolve<IRobustRandom>();
        plantholder.Seed.IdealLight = random.NextFloat(MinValue, MaxValue);
    }

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "TODO";
    }
}

/// <summary>
///     Changes the value on a plant's LightTolerance value randomly when applied.
/// </summary>
public sealed partial class PlantMutateLightTolerance : EntityEffect
{
    [DataField]
    public float MinValue = 1f;
    [DataField]
    public float MaxValue = 5f;

    public override void Effect(EntityEffectBaseArgs args)
    {
        var plantholder = args.EntityManager.GetComponent<PlantHolderComponent>(args.TargetEntity);

        if (plantholder.Seed == null)
            return;

        var random = IoCManager.Resolve<IRobustRandom>();
        plantholder.Seed.LightTolerance = random.NextFloat(MinValue, MaxValue);
    }

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "TODO";
    }
}

/// <summary>
///     Changes the value on a plant's ToxinsTolerance value randomly when applied.
/// </summary>
public sealed partial class PlantMutateToxinsTolerance : EntityEffect
{
    [DataField]
    public float MinValue = 1f;
    [DataField]
    public float MaxValue = 10f;

    public override void Effect(EntityEffectBaseArgs args)
    {
        var plantholder = args.EntityManager.GetComponent<PlantHolderComponent>(args.TargetEntity);

        if (plantholder.Seed == null)
            return;

        var random = IoCManager.Resolve<IRobustRandom>();
        plantholder.Seed.ToxinsTolerance = random.NextFloat(MinValue, MaxValue);
    }

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "TODO";
    }
}

/// <summary>
///     Changes the value on a plant's LowPressureTolerance value randomly when applied.
/// </summary>
public sealed partial class PlantMutateLowPressureTolerance : EntityEffect
{
    [DataField]
    public float MinValue = 60f;
    [DataField]
    public float MaxValue = 100f;

    public override void Effect(EntityEffectBaseArgs args)
    {
        var plantholder = args.EntityManager.GetComponent<PlantHolderComponent>(args.TargetEntity);

        if (plantholder.Seed == null)
            return;

        var random = IoCManager.Resolve<IRobustRandom>();
        plantholder.Seed.LowPressureTolerance = random.NextFloat(MinValue, MaxValue);
    }

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "TODO";
    }
}

/// <summary>
///     Changes the value on a plant's HighPressureTolerance value randomly when applied.
/// </summary>
public sealed partial class PlantMutateHighPressureTolerance : EntityEffect
{
    [DataField]
    public float MinValue = 100f;
    [DataField]
    public float MaxValue = 140f;

    public override void Effect(EntityEffectBaseArgs args)
    {
        var plantholder = args.EntityManager.GetComponent<PlantHolderComponent>(args.TargetEntity);

        if (plantholder.Seed == null)
            return;

        var random = IoCManager.Resolve<IRobustRandom>();
        plantholder.Seed.HighPressureTolerance = random.NextFloat(MinValue, MaxValue);
    }

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "TODO";
    }
}

/// <summary>
///     Changes the value on a plant's PestTolerance value randomly when applied.
/// </summary>
public sealed partial class PlantMutatePestTolerance : EntityEffect
{
    [DataField]
    public float MinValue = 0f;
    [DataField]
    public float MaxValue = 15f;

    public override void Effect(EntityEffectBaseArgs args)
    {
        var plantholder = args.EntityManager.GetComponent<PlantHolderComponent>(args.TargetEntity);

        if (plantholder.Seed == null)
            return;

        var random = IoCManager.Resolve<IRobustRandom>();
        plantholder.Seed.PestTolerance = random.NextFloat(MinValue, MaxValue);
    }

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "TODO";
    }
}

/// <summary>
///     Changes the value on a plant's WeedTolerance value randomly when applied.
/// </summary>
public sealed partial class PlantMutateWeedTolerance : EntityEffect
{
    [DataField]
    public float MinValue = 0f;
    [DataField]
    public float MaxValue = 15f;

    public override void Effect(EntityEffectBaseArgs args)
    {
        var plantholder = args.EntityManager.GetComponent<PlantHolderComponent>(args.TargetEntity);

        if (plantholder.Seed == null)
            return;

        var random = IoCManager.Resolve<IRobustRandom>();
        plantholder.Seed.WeedTolerance = random.NextFloat(MinValue, MaxValue);
    }

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "TODO";
    }
}

/// <summary>
///     Changes the value on a plant's Endurance value randomly when applied.
/// </summary>
public sealed partial class PlantMutateEndurance : EntityEffect
{
    [DataField]
    public float MinValue = 50f;
    [DataField]
    public float MaxValue = 150f;

    public override void Effect(EntityEffectBaseArgs args)
    {
        var plantholder = args.EntityManager.GetComponent<PlantHolderComponent>(args.TargetEntity);

        if (plantholder.Seed == null)
            return;

        var random = IoCManager.Resolve<IRobustRandom>();
        plantholder.Seed.Endurance = random.NextFloat(MinValue, MaxValue);
    }

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "TODO";
    }
}

/// <summary>
///     Changes the value on a plant's Yield value randomly when applied.
/// </summary>
public sealed partial class PlantMutateYield : EntityEffect
{
    [DataField]
    public int MinValue = 3;
    [DataField]
    public int MaxValue = 10;

    public override void Effect(EntityEffectBaseArgs args)
    {
        var plantholder = args.EntityManager.GetComponent<PlantHolderComponent>(args.TargetEntity);

        if (plantholder.Seed == null)
            return;

        var random = IoCManager.Resolve<IRobustRandom>();
        plantholder.Seed.Yield = random.Next(MinValue, MaxValue);
    }

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "TODO";
    }
}

/// <summary>
///     Changes the value on a plant's Lifespan value randomly when applied.
/// </summary>
public sealed partial class PlantMutateLifespan : EntityEffect
{
    [DataField]
    public float MinValue = 10f;
    [DataField]
    public float MaxValue = 80f;

    public override void Effect(EntityEffectBaseArgs args)
    {
        var plantholder = args.EntityManager.GetComponent<PlantHolderComponent>(args.TargetEntity);

        if (plantholder.Seed == null)
            return;

        var random = IoCManager.Resolve<IRobustRandom>();
        plantholder.Seed.Lifespan = random.NextFloat(MinValue, MaxValue);
    }

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "TODO";
    }
}

/// <summary>
///     Changes the value on a plant's Maturation value randomly when applied.
/// </summary>
public sealed partial class PlantMutateMaturation : EntityEffect
{
    [DataField]
    public float MinValue = 3f;
    [DataField]
    public float MaxValue = 8f;

    public override void Effect(EntityEffectBaseArgs args)
    {
        var plantholder = args.EntityManager.GetComponent<PlantHolderComponent>(args.TargetEntity);

        if (plantholder.Seed == null)
            return;

        var random = IoCManager.Resolve<IRobustRandom>();
        plantholder.Seed.Maturation = random.NextFloat(MinValue, MaxValue);
    }

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "TODO";
    }
}

/// <summary>
///     Changes the value on a plant's Production value randomly when applied.
/// </summary>
public sealed partial class PlantMutateProduction : EntityEffect
{
    [DataField]
    public float MinValue = 1f;
    [DataField]
    public float MaxValue = 10f;

    public override void Effect(EntityEffectBaseArgs args)
    {
        var plantholder = args.EntityManager.GetComponent<PlantHolderComponent>(args.TargetEntity);

        if (plantholder.Seed == null)
            return;

        var random = IoCManager.Resolve<IRobustRandom>();
        plantholder.Seed.Production = random.NextFloat(MinValue, MaxValue);
    }

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "TODO";
    }
}

/// <summary>
///     Changes the value on a plant's Potency value randomly when applied.
/// </summary>
public sealed partial class PlantMutatePotency : EntityEffect
{
    [DataField]
    public float MinValue = 30f;
    [DataField]
    public float MaxValue = 100f;

    public override void Effect(EntityEffectBaseArgs args)
    {
        var plantholder = args.EntityManager.GetComponent<PlantHolderComponent>(args.TargetEntity);

        if (plantholder.Seed == null)
            return;

        var random = IoCManager.Resolve<IRobustRandom>();
        plantholder.Seed.Potency = random.NextFloat(MinValue, MaxValue);
    }

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "TODO";
    }
}

/// <summary>
///     Changes if the plant has seeds or not.
/// </summary>
public sealed partial class PlantMutateSeedless : EntityEffect
{
    public override void Effect(EntityEffectBaseArgs args)
    {
        var plantholder = args.EntityManager.GetComponent<PlantHolderComponent>(args.TargetEntity);

        if (plantholder.Seed == null)
            return;

        plantholder.Seed.Seedless = !plantholder.Seed.Seedless;
    }

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "TODO";
    }
}

/// <summary>
///     Changes if the plant requires a hatchet to harvest.
/// </summary>
public sealed partial class PlantMutateLigneous : EntityEffect
{
    public override void Effect(EntityEffectBaseArgs args)
    {
        var plantholder = args.EntityManager.GetComponent<PlantHolderComponent>(args.TargetEntity);

        if (plantholder.Seed == null)
            return;

        plantholder.Seed.Ligneous = !plantholder.Seed.Ligneous;
    }

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "TODO";
    }
}

/// <summary>
///     Changes if the plant will turn into kudzu once weed levels are high
/// </summary>
public sealed partial class PlantMutateKudzu : EntityEffect
{
    public override void Effect(EntityEffectBaseArgs args)
    {
        var plantholder = args.EntityManager.GetComponent<PlantHolderComponent>(args.TargetEntity);

        if (plantholder.Seed == null)
            return;

        plantholder.Seed.TurnIntoKudzu = !plantholder.Seed.TurnIntoKudzu;
    }

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "TODO";
    }
}

/// <summary>
///     Changes if the plant and its produce scream.
/// </summary>
public sealed partial class PlantMutateScream : EntityEffect
{
    public override void Effect(EntityEffectBaseArgs args)
    {
        var plantholder = args.EntityManager.GetComponent<PlantHolderComponent>(args.TargetEntity);

        if (plantholder.Seed == null)
            return;

        plantholder.Seed.CanScream = !plantholder.Seed.CanScream;
    }

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "TODO";
    }
}
