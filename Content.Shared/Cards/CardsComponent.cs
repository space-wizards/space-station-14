using System.Linq;
using Robust.Shared.GameStates;

namespace Content.Shared.Cards;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class CardsComponent : Component
{
    /// <summary>
    /// The list of cards currently in this deck, in order.
    /// </summary>
    [DataField(required: true, customTypeSerializer: typeof(CardDataSerializer)), AutoNetworkedField]
    public List<CardData> Cards = new();

    /// <summary>
    /// The number of cards currently in this deck.
    /// </summary>
    [ViewVariables]
    public int NumberOfCards => Cards.Count;

    /// <summary>
    /// The prototype IDs of all cards currently in this deck.
    /// </summary>
    [ViewVariables]
    public List<string> CardPrototypes => Cards.Select(c => (string)c.CardId).ToList();

    /// <summary>
    /// Whether the deck is flipped. If <c>true</c>, the cards are face-side up.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Flipped;

    /// <summary>
    /// Whether the deck is currently displayed fanned out.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Fanned;

    /// <summary>
    /// The maximum number of cards that will be shown at once while the deck is fanned.
    /// </summary>
    /// <remarks>
    /// Large values will use a lot of sprite layers on the client.
    /// </remarks>
    [DataField, AutoNetworkedField]
    public int MaxFanned = 10;

    /// <summary>
    /// Whether a specific card is currently being taken from this deck, preventing automatic
    /// merge/split logic from pulling from the top or bottom of the deck instead.
    /// </summary>
    internal bool BeingCherryPicked;

    /// <summary>
    /// The base sprite state used for the whole deck. May be overridden by individual card prototypes.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string BaseState = "sc_base";

    /// <summary>
    /// The back sprite state used for the whole deck. Cannot be overridden by individual card prototypes.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string CardBack = "sc_backside";
}
