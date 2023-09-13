using Content.Shared.Store;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Traitor.Uplink.SurplusBundle;

/// <summary>
///     Fill crate with a random uplink items.
/// </summary>
[RegisterComponent]
public sealed partial class SurplusBundleComponent : Component
{
    /// <summary>
    ///     Total price of all content inside bundle.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    [DataField("totalPrice")]
    public int TotalPrice = 20;

    /// <summary>
    ///     The preset that will be used to get all the listings.
    ///     Currently just defaults to the basic uplink.
    /// </summary>
    [DataField("storePreset", customTypeSerializer: typeof(PrototypeIdSerializer<StorePresetPrototype>))]
    public string StorePreset = "StorePresetUplink";
}
