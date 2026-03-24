namespace Content.Shared.Store;

/// <summary>
/// Shared contract for category prototypes that participate in weighted random selection.
/// Implemented by <see cref="DiscountCategoryPrototype"/> and <see cref="SecondHandCategoryPrototype"/>.
/// </summary>
public interface IWeightedCategory
{
    /// <summary>Relative weight used during category selection rolls.</summary>
    int Weight { get; }

    /// <summary>
    /// Maximum items that may be drawn from this category per selection pass.
    /// Null means no per-category limit.
    /// </summary>
    int? MaxItems { get; }
}
