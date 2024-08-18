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
    public float minValue = 0.3f;
    [DataField]
    public float maxValue = 0.9f;

    public override void Effect(EntityEffectBaseArgs args)
    {
        var plantholder = args.EntityManager.GetComponent<PlantHolderComponent>(args.TargetEntity);

        if (plantholder.Seed == null)
            return;

        var random = IoCManager.Resolve<IRobustRandom>();
        plantholder.Seed.WaterConsumption = random.NextFloat(minValue, maxValue);

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
    public float minValue = 0.05f;
    [DataField]
    public float maxValue = 1.2f;

    public override void Effect(EntityEffectBaseArgs args)
    {
        var plantholder = args.EntityManager.GetComponent<PlantHolderComponent>(args.TargetEntity);

        if (plantholder.Seed == null)
            return;

        var random = IoCManager.Resolve<IRobustRandom>();
        plantholder.Seed.NutrientConsumption = random.NextFloat(minValue, maxValue);

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
    public float minValue = 263f;
    [DataField]
    public float maxValue = 323f;

    public override void Effect(EntityEffectBaseArgs args)
    {
        var plantholder = args.EntityManager.GetComponent<PlantHolderComponent>(args.TargetEntity);

        if (plantholder.Seed == null)
            return;

        var random = IoCManager.Resolve<IRobustRandom>();
        plantholder.Seed.IdealHeat = random.NextFloat(minValue, maxValue);

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
    public float minValue = 2f;
    [DataField]
    public float maxValue = 25f;

    public override void Effect(EntityEffectBaseArgs args)
    {
        var plantholder = args.EntityManager.GetComponent<PlantHolderComponent>(args.TargetEntity);

        if (plantholder.Seed == null)
            return;

        var random = IoCManager.Resolve<IRobustRandom>();
        plantholder.Seed.HeatTolerance = random.NextFloat(minValue, maxValue);

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
    public float minValue = 0f;
    [DataField]
    public float maxValue = 14f;

    public override void Effect(EntityEffectBaseArgs args)
    {
        var plantholder = args.EntityManager.GetComponent<PlantHolderComponent>(args.TargetEntity);

        if (plantholder.Seed == null)
            return;

        var random = IoCManager.Resolve<IRobustRandom>();
        plantholder.Seed.IdealLight = random.NextFloat(minValue, maxValue);

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
    public float minValue = 1f;
    [DataField]
    public float maxValue = 5f;

    public override void Effect(EntityEffectBaseArgs args)
    {
        var plantholder = args.EntityManager.GetComponent<PlantHolderComponent>(args.TargetEntity);

        if (plantholder.Seed == null)
            return;

        var random = IoCManager.Resolve<IRobustRandom>();
        plantholder.Seed.LightTolerance = random.NextFloat(minValue, maxValue);

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
    public float minValue = 1f;
    [DataField]
    public float maxValue = 10f;

    public override void Effect(EntityEffectBaseArgs args)
    {
        var plantholder = args.EntityManager.GetComponent<PlantHolderComponent>(args.TargetEntity);

        if (plantholder.Seed == null)
            return;

        var random = IoCManager.Resolve<IRobustRandom>();
        plantholder.Seed.ToxinsTolerance = random.NextFloat(minValue, maxValue);

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
    public float minValue = 60f;
    [DataField]
    public float maxValue = 100f;

    public override void Effect(EntityEffectBaseArgs args)
    {
        var plantholder = args.EntityManager.GetComponent<PlantHolderComponent>(args.TargetEntity);

        if (plantholder.Seed == null)
            return;

        var random = IoCManager.Resolve<IRobustRandom>();
        plantholder.Seed.LowPressureTolerance = random.NextFloat(minValue, maxValue);

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
    public float minValue = 100f;
    [DataField]
    public float maxValue = 140f;

    public override void Effect(EntityEffectBaseArgs args)
    {
        var plantholder = args.EntityManager.GetComponent<PlantHolderComponent>(args.TargetEntity);

        if (plantholder.Seed == null)
            return;

        var random = IoCManager.Resolve<IRobustRandom>();
        plantholder.Seed.HighPressureTolerance = random.NextFloat(minValue, maxValue);

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
    public float minValue = 0f;
    [DataField]
    public float maxValue = 15f;

    public override void Effect(EntityEffectBaseArgs args)
    {
        var plantholder = args.EntityManager.GetComponent<PlantHolderComponent>(args.TargetEntity);

        if (plantholder.Seed == null)
            return;

        var random = IoCManager.Resolve<IRobustRandom>();
        plantholder.Seed.PestTolerance = random.NextFloat(minValue, maxValue);

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
    public float minValue = 0f;
    [DataField]
    public float maxValue = 15f;

    public override void Effect(EntityEffectBaseArgs args)
    {
        var plantholder = args.EntityManager.GetComponent<PlantHolderComponent>(args.TargetEntity);

        if (plantholder.Seed == null)
            return;

        var random = IoCManager.Resolve<IRobustRandom>();
        plantholder.Seed.WeedTolerance = random.NextFloat(minValue, maxValue);

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
    public float minValue = 50f;
    [DataField]
    public float maxValue = 150f;

    public override void Effect(EntityEffectBaseArgs args)
    {
        var plantholder = args.EntityManager.GetComponent<PlantHolderComponent>(args.TargetEntity);

        if (plantholder.Seed == null)
            return;

        var random = IoCManager.Resolve<IRobustRandom>();
        plantholder.Seed.Endurance = random.NextFloat(minValue, maxValue);

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
    public int minValue = 3;
    [DataField]
    public int maxValue = 10;

    public override void Effect(EntityEffectBaseArgs args)
    {
        var plantholder = args.EntityManager.GetComponent<PlantHolderComponent>(args.TargetEntity);

        if (plantholder.Seed == null)
            return;

        var random = IoCManager.Resolve<IRobustRandom>();
        plantholder.Seed.Yield = random.Next(minValue, maxValue);

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
    public float minValue = 10f;
    [DataField]
    public float maxValue = 80f;

    public override void Effect(EntityEffectBaseArgs args)
    {
        var plantholder = args.EntityManager.GetComponent<PlantHolderComponent>(args.TargetEntity);

        if (plantholder.Seed == null)
            return;

        var random = IoCManager.Resolve<IRobustRandom>();
        plantholder.Seed.Lifespan = random.NextFloat(minValue, maxValue);
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
    public float minValue = 3f;
    [DataField]
    public float maxValue = 8f;

    public override void Effect(EntityEffectBaseArgs args)
    {
        var plantholder = args.EntityManager.GetComponent<PlantHolderComponent>(args.TargetEntity);

        if (plantholder.Seed == null)
            return;

        var random = IoCManager.Resolve<IRobustRandom>();
        plantholder.Seed.Maturation = random.NextFloat(minValue, maxValue);
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
    public float minValue = 1f;
    [DataField]
    public float maxValue = 10f;

    public override void Effect(EntityEffectBaseArgs args)
    {
        var plantholder = args.EntityManager.GetComponent<PlantHolderComponent>(args.TargetEntity);

        if (plantholder.Seed == null)
            return;

        var random = IoCManager.Resolve<IRobustRandom>();
        plantholder.Seed.Production = random.NextFloat(minValue, maxValue);
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
    public float minValue = 30f;
    [DataField]
    public float maxValue = 100f;

    public override void Effect(EntityEffectBaseArgs args)
    {
        var plantholder = args.EntityManager.GetComponent<PlantHolderComponent>(args.TargetEntity);

        if (plantholder.Seed == null)
            return;

        var random = IoCManager.Resolve<IRobustRandom>();
        plantholder.Seed.Potency = random.NextFloat(minValue, maxValue);
    }

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "TODO";
    }
}

