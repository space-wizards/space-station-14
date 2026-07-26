using Robust.Shared.GameStates;

namespace Content.Shared.Botany.Traits.Components;

/// <summary>
/// Base component for plant trait components.
/// </summary>
[RegisterComponent, NetworkedComponent]
public abstract partial class PlantTraitsComponent : Component
{
    /// <summary>
    /// Localization key describing the plant trait state.
    /// </summary>
    public abstract LocId TraitState { get; set; }
}
