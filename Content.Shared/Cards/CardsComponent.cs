using System.Linq;
using JetBrains.Annotations;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Cards;

/// <summary>
/// Handles the card information in a deck of cards.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class CardsComponent : Component
{
    /// <summary>
    /// The list of cards currently in this deck, in order.
    /// </summary>
    [DataField(customTypeSerializer: typeof(CardDataSerializer)), AutoNetworkedField]
    public List<CardData> Cards = new();

    /// <summary>
    /// What stack type we are.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<CardStackPrototype> CardStackType = default!;

    /// <summary>
    /// Max amount of things that can be in the stack.
    /// Overrides the max defined on the card stack prototype.
    /// </summary>
    [DataField]
    public int? MaxCountOverride;

    /// <summary>
    /// Sprite layers used in card visualizer. Sprites first in layer correspond to lower stack states
    /// e.g. <code>_spriteLayers[0]</code> is lower stack level than <code>_spriteLayers[1]</code>.
    /// </summary>
    [DataField]
    public List<string> LayerStates = new();

    /// <summary>
    ///     layer for layerStates sprite.
    /// </summary>
    [DataField]
    public string BaseLayer = "base";

    /// <summary>
    /// A list of thresholds to check against the number of things in the deck.
    /// Each exceeded threshold will cause the next layer to be displayed.
    /// Should be sorted in ascending order.
    /// </summary>
    [DataField(required: true)]
    public List<int> Thresholds;

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
    /// The base sprite state used for the whole deck. May be overridden by individual card prototypes.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string BaseState = "sc_base";

    /// <summary>
    /// The back sprite state used for the whole deck. Cannot be overridden by individual card prototypes.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string CardBack = "sc_backside";

    /// <summary>
    /// The number of cards currently in this deck. Used for debugging only.
    /// </summary>
    [ViewVariables, UsedImplicitly]
    private int NumberOfCards => Cards.Count;

    /// <summary>
    /// The prototype IDs of all cards currently in this deck. Used for debugging only.
    /// </summary>
    [ViewVariables, UsedImplicitly]
    private List<string> CardPrototypes => Cards.Select(c => (string)c.CardId).ToList();

}
