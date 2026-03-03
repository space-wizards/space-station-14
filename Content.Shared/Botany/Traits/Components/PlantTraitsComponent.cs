using Robust.Shared.GameStates;

namespace Content.Shared.Botany.Traits.Components;

/// <summary>
/// Base component for plant trait components.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Virtual]
public partial class PlantTraitsComponent : Component
{
    public virtual IEnumerable<string> GetTraitStateMarkup()
    {
        yield break;
    }
}
