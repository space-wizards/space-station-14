using Robust.Shared.Prototypes;

namespace Content.Server.Botany;

[Prototype("transmutation")]
public sealed class TransmuationPrototype : IPrototype
{
    [IdDataField] public string ID { get; private init; } = default!;

    [DataField("plantPrototype")] public string prototype = "";

    [DataField("t")] public int T = 0;
    [DataField("r")] public int R = 0;
    [DataField("a")] public int A = 0;
}

