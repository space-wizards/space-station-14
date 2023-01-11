using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Dictionary;

namespace Content.Shared.Medical.Circulation.Components;

[RegisterComponent]
public sealed class CirculationVesselComponent : Component
{
    //The parent circulationSystem
    public EntityUid Parent = EntityUid.Invalid;

    //Reagents that are present in the vessel, these get synced with the rest of the circulation system when it updates
    [DataField("localReagents",
        customTypeSerializer: typeof(PrototypeIdDictionarySerializer<FixedPoint2, ReagentPrototype>))]
    public Dictionary<string, FixedPoint2>? LocalReagents = null;

    //TODO: convert this to use a list instead of a dict

    //Loose capacity of the vessel, used for calculating circulation volume/pressure. This can be exceeded
    [DataField("capacity", required: true)]
    public FixedPoint2 Capacity;
}

[Serializable, NetSerializable]
public sealed class CirculationVesselComponentState : ComponentState
{
}
