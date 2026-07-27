namespace Content.Shared.Botany.Traits.Components;

/// <summary>
/// Base class for plant trait components.
/// </summary>
public abstract partial class PlantTraitsComponent : Component
{
    /// <summary>
    /// Localization key describing the plant trait state.
    /// </summary>
    [DataField]
    public abstract LocId TraitState { get; set; }
}
