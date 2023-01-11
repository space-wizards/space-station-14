using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Dictionary;

namespace Content.Shared.Medical.Circulation.Components;

[RegisterComponent]
public sealed class CirculationComponent : Component
{
    //Primary reagents affect circulation pressure
    [DataField("reagents", required: true,
        customTypeSerializer: typeof(PrototypeIdDictionarySerializer<FixedPoint2, ReagentPrototype>))]
    public Dictionary<string, FixedPoint2> Reagents = new();

    //Linked circulatory vessels
    public HashSet<EntityUid> LinkedVessels = new();

    //Cached total Reagent volume, this is updated anytime a reagent's volume is updated
    public FixedPoint2 TotalReagentVolume;

    //Cached total capacity of the circulatory system,
    //This is updated anytime a circulatory vessel is added/removed/updated and is used to calculate how much of
    //each reagent to transfer to the localReagentVolume if a vessel is separated from the system
    public FixedPoint2 TotalCapacity;
}

[Serializable, NetSerializable]
public sealed class CirculationComponentState : ComponentState
{
    public Dictionary<string, FixedPoint2> Reagents;
    public FixedPoint2 TotalReagentVolume;
    public HashSet<EntityUid> LinkedVessels;
    public FixedPoint2 TotalCapacity;

    public CirculationComponentState(Dictionary<string, FixedPoint2> reagents,HashSet<EntityUid> linkedVessels ,FixedPoint2 totalReagentVolume,
        FixedPoint2 totalCapacity)
    {
        LinkedVessels = linkedVessels;
        TotalReagentVolume = totalReagentVolume;
        Reagents = reagents;
        TotalCapacity = totalCapacity;
    }
}
