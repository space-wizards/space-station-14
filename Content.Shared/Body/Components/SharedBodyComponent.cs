using Content.Shared.Body.Part;
using Content.Shared.Body.Prototypes;
using Content.Shared.Body.Systems.Body;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Body.Components;

[NetworkedComponent]
[Access(typeof(SharedBodySystem))]
public abstract class SharedBodyComponent : Component
{
    [DataField("template", required: true, customTypeSerializer: typeof(PrototypeIdSerializer<BodyTemplatePrototype>))]
    public string TemplateId = default!;

    [DataField("preset", required: true, customTypeSerializer: typeof(PrototypeIdSerializer<BodyPresetPrototype>))]
    public string PresetId = default!;

    [ViewVariables]
    public Dictionary<string, BodyPartSlot> Slots = new();
}

[Serializable, NetSerializable]
public sealed class BodyComponentState : ComponentState
{
    public readonly Dictionary<string, BodyPartSlot> Slots;

    public BodyComponentState(Dictionary<string, BodyPartSlot> slots)
    {
        Slots = slots;
    }
}
