using Content.Shared.Eui;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using static Robust.Shared.Utility.SpriteSpecifier;

namespace Content.Shared._Starlight.Railroading;

[NetSerializable, Serializable]
public sealed class CardSelectionEuiState : EuiStateBase
{
    public List<Card> Cards { get; set; } = [];
}
[NetSerializable, Serializable]
public sealed class Card
{
    public required NetEntity Id { get; init; }
    public string Title { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public Color Color { get; set; }
    public Color IconColor { get; set; }
    public string Description { get; set; } = string.Empty;
    public Texture? Image { get; set; }
}

[Serializable, NetSerializable]
public sealed class CardSelectedMessage : EuiMessageBase
{
    public NetEntity Card { get; init; }

}
[Serializable, NetSerializable]
public sealed class CardSelectionClosedMessage : EuiMessageBase
{
}