using Content.Server.Botany.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Server.Botany.Systems;

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

    [Dependency] private readonly PlantHolderSystem _plantHolder = default!;
    [Dependency] private readonly PlantTraySystem _plantTray = default!;
    [Dependency] private readonly PlantTraitsSystem _plantTrait = default!;
    [Dependency] private readonly TransformSystem _transform = default!;

    public override void OnPlantGrow(Entity<PlantTraitsComponent> ent, ref OnPlantGrowEvent args)
    {
        if (args.Tray.Comp == null)
            return;

        if (args.Tray.Comp is { WaterLevel: > 10, NutritionLevel: > 5 })
            _plantTray.AdjustWeed(args.Tray.AsNullable(), 1);

        // Handle kudzu transformation.
        if (args.Tray.Comp.WeedLevel >= WeedLevelThreshold)
        {
            Spawn(KudzuPrototype, _transform.GetMapCoordinates(ent.Owner));
            _plantTrait.DelTrait(ent.AsNullable(), this);
            _plantHolder.Die(ent.Owner);
        }
    }

    public override IEnumerable<string> GetPlantStateMarkup()
    {
        yield return Loc.GetString("mutation-plant-kudzu");
    }
}
