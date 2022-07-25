using Content.Shared.Body.Part;
using Content.Shared.Body.Prototypes;
using Content.Shared.Body.Systems.Body;
using Robust.Shared.Containers;
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
    public Dictionary<string, BodyPartSlot> SlotIds = new();

    [ViewVariables]
    public Dictionary<SharedBodyPartComponent, BodyPartSlot> Parts = new();

    [ViewVariables]
    public Container PartContainer = default!;
}

[Serializable]
[NetSerializable]
public class BodyComponentState : ComponentState
{
    public Dictionary<string, EntityUid> Parts;

    public BodyComponentState(Dictionary<string, EntityUid> parts)
    {
        Parts = parts;
    }
}
