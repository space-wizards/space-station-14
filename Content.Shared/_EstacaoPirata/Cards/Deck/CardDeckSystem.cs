using Content.Shared._EstacaoPirata.Cards.Stack;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._EstacaoPirata.Cards.Deck;

/// <summary>
/// This handles card decks
///
/// </summary>
public sealed class CardDeckSystem : EntitySystem
{
    [Dependency] private readonly CardStackSystem _cardStackSystem = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    public readonly EntProtoId CardDeckBaseName = "CardDeckBase";

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<CardDeckComponent, GetVerbsEvent<AlternativeVerb>>(AddTurnOnVerb);
    }

    private void AddTurnOnVerb(EntityUid uid, CardDeckComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || args.Hands == null)
            return;

        if (!TryComp(uid, out CardStackComponent? comp))
            return;

        args.Verbs.Add(new AlternativeVerb()
        {
            Act = () => TryShuffle(uid, component, comp),
            Text = Loc.GetString("cards-verb-shuffle"),
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/die.svg.192dpi.png")),
            Priority = 4
        });
        args.Verbs.Add(new AlternativeVerb()
        {
            Act = () => TrySplit(args.Target, component, comp, args.User),
            Text = Loc.GetString("cards-verb-split"),
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/dot.svg.192dpi.png")),
            Priority = 3
        });
        args.Verbs.Add(new AlternativeVerb()
        {
            Act = () => TryOrganize(uid, component, comp, true),
            Text = Loc.GetString("cards-verb-organize-down"),
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/flip.svg.192dpi.png")),
            Priority = 2
        });
        args.Verbs.Add(new AlternativeVerb()
        {
            Act = () => TryOrganize(uid, component, comp, false),
            Text = Loc.GetString("cards-verb-organize-up"),
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/flip.svg.192dpi.png")),
            Priority = 1
        });
    }

    private void TrySplit(EntityUid uid, CardDeckComponent deck, CardStackComponent stack, EntityUid user)
    {
        if (stack.Cards.Count <= 1)
            return;

        _audio.PlayPredicted(deck.PickUpSound, Transform(uid).Coordinates, user);

        if (!_net.IsServer)
            return;

        var cardDeck = SpawnInSameParent(CardDeckBaseName, uid);

        EnsureComp<CardStackComponent>(cardDeck, out var deckStack);

        _cardStackSystem.TransferNLastCardFromStacks(user, stack.Cards.Count / 2, uid, stack, cardDeck, deckStack);
        _hands.PickupOrDrop(user, cardDeck);
    }

    private void TryShuffle(EntityUid deck, CardDeckComponent comp, CardStackComponent? stack)
    {
        _cardStackSystem.ShuffleCards(deck, stack);
        if (_net.IsClient)
            return;

        _audio.PlayPvs(comp.ShuffleSound, deck, AudioParams.Default.WithVariation(0.05f));
        _popup.PopupEntity(Loc.GetString("card-verb-shuffle-success", ("target", MetaData(deck).EntityName)), deck);
    }

    private void TryOrganize(EntityUid deck, CardDeckComponent comp, CardStackComponent? stack, bool isFlipped)
    {
        if (_net.IsClient)
            return;
        _cardStackSystem.FlipAllCards(deck, stack, isFlipped: isFlipped);

        _audio.PlayPvs(comp.ShuffleSound, deck, AudioParams.Default.WithVariation(0.05f));
        _popup.PopupEntity(Loc.GetString("card-verb-organize-success", ("target", MetaData(deck).EntityName), ("facedown", isFlipped)), deck);
    }

    private EntityUid SpawnInSameParent(string prototype, EntityUid uid)
    {
        if (_container.IsEntityOrParentInContainer(uid) &&
            _container.TryGetOuterContainer(uid, Transform(uid), out var container))
        {
            return SpawnInContainerOrDrop(prototype, container.Owner, container.ID);
        }
        return Spawn(prototype, Transform(uid).Coordinates);
    }
}
