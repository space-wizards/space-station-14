using Content.Shared.Store;
using Robust.Shared.Prototypes;

namespace Content.Server.Objectives.Components;

/// <summary>
/// Unlocks store listings that use <see cref="ObjectiveUnlockCondition"/>.
/// </summary>
[RegisterComponent]
public sealed partial class StoreUnlockerComponent : Component
{
    [DataField(required: true)]
    public List<ProtoId<ListingPrototype>> Listings = new();
}
