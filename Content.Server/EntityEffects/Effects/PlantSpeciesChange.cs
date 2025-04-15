using Content.Server.Botany;
using Content.Server.Botany.Components;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Serilog;

namespace Content.Server.EntityEffects.Effects;

/// <summary>
///     Changes a plant into one of the species its able to mutate into.
/// </summary>
public sealed partial class PlantSpeciesChange : EntityEffect
{
    public override void Effect(EntityEffectBaseArgs args)
    {
        var prototypeManager = IoCManager.Resolve<IPrototypeManager>();
        var plantholder = args.EntityManager.GetComponent<PlantHolderComponent>(args.TargetEntity);

        if (plantholder.Seed == null)
            return;

        if (plantholder.Seed.MutationPrototypes.Count == 0)
            return;

        var random = IoCManager.Resolve<IRobustRandom>();
        var targetProto = random.Pick(plantholder.Seed.MutationPrototypes);
        prototypeManager.TryIndex(targetProto, out SeedPrototype? protoSeed);

        if (protoSeed == null)
        {
            Log.Error($"Seed prototype could not be found: {targetProto}!");
            return;
        }

        plantholder.Seed = plantholder.Seed.SpeciesChange(protoSeed);
    }

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "TODO";
    }
}
