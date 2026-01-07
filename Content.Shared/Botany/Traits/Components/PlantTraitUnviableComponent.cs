using Robust.Shared.GameStates;

namespace Content.Shared.Botany.Traits.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class PlantTraitUnviableComponent : PlantTraitsComponent
{
    /// <summary>
    /// Amount of damage dealt to the plant per growth tick with unviable.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float UnviableDamage = 6f;

    public override IEnumerable<string> GetTraitStateMarkup()
    {
        yield return Loc.GetString("mutation-plant-unviable");
    }
}
