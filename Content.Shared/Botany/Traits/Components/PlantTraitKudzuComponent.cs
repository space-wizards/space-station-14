using Robust.Shared.Prototypes;
using Robust.Shared.GameStates;

namespace Content.Shared.Botany.Traits.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class PlantTraitKudzuComponent : PlantTraitsComponent
{
    /// <summary>
    /// Which kind of kudzu this plant will turn into if it kuzuifies.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntProtoId KudzuPrototype = "WeakKudzu";

    /// <summary>
    /// Weed level threshold at which the plant is considered overgrown and will transform into kudzu.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float WeedLevelThreshold = 10f;

    /// <summary>
    /// Amount of weed to grow per growth tick.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float WeedGrowthAmount = 1f;

    public override IEnumerable<string> GetTraitStateMarkup()
    {
        yield return Loc.GetString("mutation-plant-kudzu");
    }
}
