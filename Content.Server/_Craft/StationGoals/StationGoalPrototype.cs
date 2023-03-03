using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Server._Craft.StationGoals;

[Serializable, Prototype("stationGoal")]
public sealed class StationGoalPrototype : IPrototype
{
    [IdDataFieldAttribute]
    public string ID { get; } = default!;

    [DataField("text")]
    public string Text { get; set; } = string.Empty;


    [DataField("cargoAdvancedProductsIDs", customTypeSerializer: typeof(PrototypeIdListSerializer<EntityPrototype>))]
    public List<string> CargoAdvancedProductsIDs { get; set; } = new ();
}

