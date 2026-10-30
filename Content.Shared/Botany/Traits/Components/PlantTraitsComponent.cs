namespace Content.Shared.Botany.Traits.Components;

/// <summary>
/// Base class for plant trait components.
/// </summary>
public abstract partial class PlantTraitsComponent : Component
{
    /// <summary>
    /// If true, pollination cannot add or remove this trait.
    /// </summary>
    [DataField]
    public virtual bool PollinationProtected { get; set; }

    /// <summary>
    /// Localization key describing the plant trait state.
    /// </summary>
    [DataField]
    public virtual LocId? TraitState { get; set; }

    /// <summary>
    /// Localization key for the name of the trait.
    /// </summary>
    [DataField]
    public virtual LocId? TraitName { get; set; }
}
