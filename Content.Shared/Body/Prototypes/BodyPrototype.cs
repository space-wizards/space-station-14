using Robust.Shared.Prototypes;

namespace Content.Shared.Body.Prototypes;

[Prototype("body")]
public sealed partial class BodyPrototype : IPrototype
{
    [IdDataField] public string ID { get; } = default!;

    [DataField("name")]
    public string Name { get; private set; } = "";

    [DataField("root")] public string Root { get; private set; } = string.Empty;

    [DataField("slots")] public Dictionary<string, BodyPrototypeSlot> Slots { get; private set; } = new();

    private BodyPrototype() { }

    public BodyPrototype(string id, string name, string root, Dictionary<string, BodyPrototypeSlot> slots)
    {
        ID = id;
        Name = name;
        Root = root;
        Slots = slots;
    }
}

[DataRecord]
public sealed record BodyPrototypeSlot(EntProtoId? Part, HashSet<string> Connections, Dictionary<string, string> Organs);
