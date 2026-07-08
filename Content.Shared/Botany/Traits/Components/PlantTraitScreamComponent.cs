using Robust.Shared.GameStates;
using Robust.Shared.Audio;

namespace Content.Shared.Botany.Traits.Components;

/// <summary>
/// A plant trait that plays screams when the plant is harvested.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class PlantTraitScreamComponent : PlantTraitsComponent
{
    /// <summary>
    /// The sound to play when the plant screams.
    /// </summary>
    [DataField, AutoNetworkedField]
    public SoundSpecifier ScreamSound = new SoundCollectionSpecifier("PlantScreams");

    public override IEnumerable<string> GetTraitStateMarkup()
    {
        yield return Loc.GetString("mutation-plant-scream");
    }
}
