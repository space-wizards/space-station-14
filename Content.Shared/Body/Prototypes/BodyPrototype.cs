using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Body.Prototypes;

[Prototype("body")]
public sealed class BodyPrototype : IPrototype
{
    [IdDataField] public string ID { get; } = default!;

    [DataField("name")]
    public string Name { get; private set; } = "";

    [DataField("root")] public string Root { get; } = string.Empty;

    [DataField("slots")] public Dictionary<string, BodyPrototypeSlot> Slots { get; } = new();

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
public sealed record BodyPrototypeSlot
{
    [DataField("part", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public readonly string? Part;
    public readonly HashSet<string> Connections = new();
    public readonly Dictionary<string, string> Organs = new();

    public BodyPrototypeSlot(string? part, HashSet<string>? connections, Dictionary<string, string>? organs)
    {
        Part = part;
        Connections = connections ?? new HashSet<string>();
        Organs = organs ?? new Dictionary<string, string>();
    }

    public void Deconstruct(out string? part, out HashSet<string> connections, out Dictionary<string, string> organs)
    {
        part = Part;
        connections = Connections;
        organs = Organs;
    }
}
