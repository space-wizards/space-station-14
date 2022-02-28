using Robust.Shared.Serialization;
using Content.Shared.Sound;

namespace Content.Shared.PlayingCard;
public abstract class SharedPlayingCardDeckComponent : Component, ISerializationHooks
{

    // Allow setting a unified deck ID if someone really wants to make a TCG that can share cards
    [DataField("cardDeckID")]
    public string CardDeckID = string.Empty;
    [DataField("cardPrototype")]
    public string CardPrototype { get; private set; } = string.Empty;
    [DataField("cardHandPrototype")]
    public string CardHandPrototype { get; private set; } = string.Empty;
    [DataField("cardList")]
    public List<string> CardList = new();
    [DataField("noUniqueCardLayers")]
    public bool NoUniqueCardLayers = false;
    [DataField("shuffleSound")]
    public SoundSpecifier ShuffleSound = new SoundPathSpecifier("/Audio/Effects/cardshuffle.ogg");
}

[Serializable, NetSerializable]
public class PickupCountMessage : BoundUserInterfaceMessage
{
    public readonly int Count;
    public PickupCountMessage(int count)
    {
        Count = count;
    }
}

[Serializable, NetSerializable]
public enum PlayingCardDeckUiKey
{
    Key
}
