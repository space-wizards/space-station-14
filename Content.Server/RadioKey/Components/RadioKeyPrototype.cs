using Robust.Shared.Prototypes;

namespace Content.Server.RadioKey.Components;

[Prototype("radioKey")]
public sealed class RadioKeyPrototype : IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; } = default!;

    /// <summary>
    /// Frequencies this radiokey unlocks
    /// </summary>
    [DataField("frequency", required: true)]
    public IReadOnlyList<int> Frequency = default!;

    // TODO flag these or something
    [DataField("syndie")]
    public bool Syndie { get; private set; } = false;

    [DataField("translateBinary")]
    public bool TranslateBinary { get; private set; } = false;
}
