using Content.Shared.Botany.Components;
using Content.Shared.Botany.Events;
using Robust.Shared.Prototypes;

namespace Content.Shared.Botany.Systems;

/// <summary>
/// As the plant grows, it increases the <see cref="PlantTrayComponent.WeedLevel"/>.
/// Once the limit is exceeded, it kills the plant and creates kudzu.
/// </summary>
[DataDefinition]
public sealed partial class TraitKudzu : PlantTrait
{
    /// <summary>
    /// Which kind of kudzu this plant will turn into if it kuzuifies.
    /// </summary>
    [DataField]
    public EntProtoId KudzuPrototype = "WeakKudzu";

    /// <summary>
    /// Weed level threshold at which the plant is considered overgrown and will transform into kudzu.
    /// </summary>
    [DataField]
    public float WeedLevelThreshold = 10f;

    /// <summary>
    /// Amount of weed to grow per growth tick.
    /// </summary>
    [DataField]
    public float WeedGrowthAmount = 1f;

    [Dependency] private readonly PlantHolderSystem _plantHolder = default!;
    [Dependency] private readonly PlantTraitsSystem _plantTrait = default!;
    [Dependency] private readonly PlantTraySystem _plantTray = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void OnPlantGrow(Entity<PlantTraitsComponent> ent, ref OnPlantGrowEvent args)
    {
        var trayUid = GetEntity(args.Tray);
        if (!TryComp<PlantTrayComponent>(trayUid, out var trayComp))
            return;

        if (trayComp is { WaterLevel: > 10, NutritionLevel: > 5 })
            _plantTray.AdjustWeed(trayUid, WeedGrowthAmount);

        // Handle kudzu transformation.
        if (trayComp.WeedLevel >= WeedLevelThreshold)
        {
            EntityManager.PredictedSpawn(KudzuPrototype, _transform.GetMapCoordinates(ent.Owner));
            _plantTrait.DelTrait(ent.AsNullable(), this);
            _plantHolder.Die(ent.Owner);
        }
    }

    public override IEnumerable<string> GetTraitStateMarkup()
    {
        yield return Loc.GetString("mutation-plant-kudzu");
    }
}
