using Robust.Shared.Prototypes;

namespace Content.Shared.Body.Prototypes;

[PrototypeRecord("body")]
public sealed record BodyPrototype(
    [field: IdDataField] string ID,
    string Name,
    string Root,
    Dictionary<string, BodyPrototypeSlot> Slots
) : IPrototype;

[DataRecord]
public sealed record BodyPrototypeSlot
{
    [DataField("part", required: true)] public readonly string Part = default!;
    public readonly HashSet<string> Connections = new();
    public readonly Dictionary<string, string> Organs = new();

    public BodyPrototypeSlot() { }

    public BodyPrototypeSlot(string part, HashSet<string>? connections, Dictionary<string, string>? organs)
    {
        Part = part;
        Connections = connections ?? new HashSet<string>();
        Organs = organs ?? new Dictionary<string, string>();
    }

    public void Deconstruct(out string part, out HashSet<string> connections, out Dictionary<string, string> organs)
    {
        part = Part;
        connections = Connections;
        organs = Organs;
    }
}
