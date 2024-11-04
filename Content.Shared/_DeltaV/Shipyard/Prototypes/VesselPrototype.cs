using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._DeltaV.Shipyard.Prototypes;

[Prototype("vessel")]
public sealed class VesselPrototype : IPrototype
{
    [ViewVariables, IdDataField]
    public string ID { get; } = default!;

    /// <summary>
    /// Already localized name of the vessel.
    /// </summary>
    [DataField(required: true)]
    public string Name = string.Empty;

    /// <summary>
    /// Already localized short description of the vessel.
    /// </summary>
    [DataField(required: true)]
    public string Description = string.Empty;

    /// <summary>
    /// How much the vessel costs to purchase.
    /// </summary>
    [DataField(required: true)]
    public int Price;

    /// <summary>
    /// Path to the shuttle yml to load, e.g. `/Maps/Shuttles/yourshittle.yml`
    /// </summary>
    [DataField(required: true)]
    public List<ResPath> Path = default!;

    /// <summary>
    /// Categories that can be filtered in the UI.
    /// </summary>
    [DataField]
    public List<ProtoId<VesselCategoryPrototype>> Categories = new();

    /// <summary>
    /// If the console does not match this whitelist, the vessel is hidden and can't be bought.
    /// </summary>
    [DataField]
    public EntityWhitelist? Whitelist;
}
