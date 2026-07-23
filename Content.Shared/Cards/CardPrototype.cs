using Robust.Shared.Prototypes;

namespace Content.Shared.Cards;

[Prototype]
public sealed partial class CardPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = string.Empty;

    [DataField]
    public string? LayerOneState { get; private set; }

    [DataField]
    public Color? LayerOneColor { get; private set; }

    [DataField]
    public string? LayerTwoState { get; private set; }

    [DataField]
    public Color? LayerTwoColor { get; private set; }

    [DataField]
    public string? BaseState { get; private set; }

    [DataField]
    public string? CardBack { get; private set; }
}
