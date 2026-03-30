using Content.Shared.Store;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.StoreDiscount.Components;

/// <summary>
/// Partner-component for adding second-hand (worn/damaged) item functionality to StoreSystem via SecondHandSystem.
/// </summary>
[RegisterComponent]
public sealed partial class StoreSecondHandComponent : Component
{
    /// <summary>
    /// Second-hand items selected for this store instance.
    /// </summary>
    [ViewVariables, DataField]
    public IReadOnlyList<StoreSecondHandData> SecondHandItems = Array.Empty<StoreSecondHandData>();
}

/// <summary>
/// Container for a second-hand listing's selection state.
/// </summary>
[Serializable, NetSerializable, DataDefinition]
public sealed partial class StoreSecondHandData
{
    /// <summary>
    /// Id of the second-hand listing that was selected.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<ListingPrototype> ListingId;

    /// <summary>
    /// How many times this second-hand item can still be purchased.
    /// Each purchase decrements this counter; item is hidden when it reaches zero.
    /// </summary>
    [DataField]
    public int Count;

    /// <summary>
    /// The second-hand category that caused this item to be selected.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<SecondHandCategoryPrototype> SecondHandCategory;
}
