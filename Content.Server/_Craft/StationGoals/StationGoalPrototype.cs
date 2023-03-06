using Content.Server._Craft.StationGoals.Scipts;
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

    [DataField("canStartAutomatic", serverOnly: true)]
    public bool CanStartAutomatic { get; set; } = true;

    [DataField("scripts", serverOnly: true)]
    private IStationGoalScript[] _scripts = Array.Empty<IStationGoalScript>();


    [DataField("cargoAdvancedProductsIDs", customTypeSerializer: typeof(PrototypeIdListSerializer<EntityPrototype>))]
    public List<string> CargoAdvancedProductsIDs { get; set; } = new();
    public IReadOnlyList<IStationGoalScript> Scripts => _scripts;
}

