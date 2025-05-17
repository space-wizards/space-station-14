using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Store.Components;

/// <summary>
/// This component is used to track which stock-limited listings are currently being processed
/// to prevent spam-clicking and duplicate purchases.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class StockLimitedProcessingComponent : Component
{
    /// <summary>
    /// Dictionary to track which listings are currently being processed.
    /// Key is the listing ID, value is whether the listing is being processed.
    /// </summary>
    [DataField("processingListings")]
    public Dictionary<string, bool> ProcessingListings = new();
}
