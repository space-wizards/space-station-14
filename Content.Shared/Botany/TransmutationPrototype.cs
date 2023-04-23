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

public sealed class TRASequence
{
    [ViewVariables(VVAccess.ReadWrite)]
    public int T;

    [ViewVariables(VVAccess.ReadWrite)]
    public int R;

    [ViewVariables(VVAccess.ReadWrite)]
    public int A;

    public TRASequence(int t,int r,int a){
        T = t;
        R = r;
        A = a;
    }
}
