using Content.Shared.Tag;
using Content.Shared.Tools;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Dictionary;

namespace Content.Server.Mech.Components;

[RegisterComponent]
public sealed class MechAssemblyComponent : Component
{
    [DataField("requiredParts", required: true, customTypeSerializer: typeof(PrototypeIdDictionarySerializer<bool, TagPrototype>))]
    public Dictionary<string, bool> RequiredParts = new();

    [DataField("finishedPrototype", required: true, customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string FinishedPrototype = default!;

    [ViewVariables]
    public Container PartsContainer = default!;

    [DataField("qualityNeeded", customTypeSerializer: typeof(PrototypeIdSerializer<ToolQualityPrototype>))]
    public string QualityNeeded = "Prying";
}
