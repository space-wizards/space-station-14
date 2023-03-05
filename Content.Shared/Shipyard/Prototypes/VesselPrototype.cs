using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Utility;

namespace Content.Shared.Shipyard.Prototypes;

[Prototype("vessel")]
public sealed class VesselPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; } = default!;

    /// <summary>
    ///     Vessel name.
    /// </summary>
    [DataField("name")] public string Name = string.Empty;

    /// <summary>
    ///     Short description of the vessel.
    /// </summary>
    [DataField("description")] public string Description = string.Empty;

    /// <summary>
    ///     The price of the vessel
    /// </summary>
    [DataField("price", required: true)]
    public int Price;

    /// <summary>
    ///     The category of the product. (e.g. Small, Medium, Large, Emergency, Special etc.)
    /// </summary>
    [DataField("category")]
    public string Category = string.Empty;

    /// <summary>
    ///     The group of the product. (e.g. Civilian, Syndicate, Contraband etc.)
    /// </summary>
    [DataField("group")]
    public string Group = string.Empty;

    /// <summary>
    ///     Relative directory path to the given shuttle, i.e. `/Maps/Shuttles/yourshittle.yml`
    /// </summary>
    [DataField("shuttlePath", required: true)]
    public ResourcePath ShuttlePath = default!;
}
