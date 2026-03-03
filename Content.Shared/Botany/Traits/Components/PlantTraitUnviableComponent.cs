using Robust.Shared.GameStates;

namespace Content.Shared.Botany.Traits.Components;

/// <summary>
/// A plant trait that causes a plant to stop growing and die quickly.
/// It adds a bit of challenge to keeping mutated plants alive via Unviable's frequency.
/// </summary>
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
