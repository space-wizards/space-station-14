using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Dictionary;

namespace Content.Shared.Medical.Circulation.Components;

[RegisterComponent]
public sealed class CirculationComponent : Component
{
    //Primary reagents affect circulation pressure
    [DataField("reagents", required: true, customTypeSerializer:typeof(PrototypeIdDictionarySerializer<FixedPoint2,ReagentPrototype>))]
    public Dictionary<string, FixedPoint2> Reagents = new();

    //How much do the reagents decrease per bleedTick. NewReagentVolume = OldReagentVolume - (TotalReagentVolume/ReagentVolume * BleedRate)
    [DataField("bleedRate")] public Dictionary<string, FixedPoint2> BleedRate = new();

    //Cached total Reagent volume, this is updated anytime a reagent's volume is updated
    public FixedPoint2 TotalReagentVolume;

    //The capacity of the circulatory system, this is used to calculate circulation pressure, the sum of all reagents that
    //contribute pressure is checked against this value
    [DataField("capacity")] public FixedPoint2 Capacity;
}

[Serializable, NetSerializable]
public sealed class CirculatoryComponentState : ComponentState
{
    public Dictionary<string, FixedPoint2> Reagents;
    public FixedPoint2 TotalReagentVolume;
    public FixedPoint2 Capacity;

    public CirculatoryComponentState(Dictionary<string, FixedPoint2> reagents, FixedPoint2 totalReagentVolume, FixedPoint2 capacity)
    {
        TotalReagentVolume = totalReagentVolume;
        Reagents = reagents;
        Capacity = capacity;
    }
}

