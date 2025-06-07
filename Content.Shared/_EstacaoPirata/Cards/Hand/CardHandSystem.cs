using System.Linq;
using Content.Shared._EstacaoPirata.Cards.Card;
using Content.Shared._EstacaoPirata.Cards.Deck;
using Content.Shared._EstacaoPirata.Cards.Stack;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Storage.EntitySystems;
using Content.Shared.Verbs;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._EstacaoPirata.Cards.Hand;

/// <summary>
/// This handles...
/// </summary>

public sealed class CardHandSystem : EntitySystem
{
    [ValidatePrototypeId<EntityPrototype>]
    public readonly EntProtoId CardHandBaseName = "CardHandBase";
    [ValidatePrototypeId<EntityPrototype>]
    public readonly EntProtoId CardDeckBaseName = "CardDeckBase";

    [Dependency] private readonly CardStackSystem _cardStack = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedStorageSystem _storage = default!; // Frontier

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<CardComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<CardHandComponent, CardHandDrawMessage>(OnCardDraw);
        SubscribeLocalEvent<CardHandComponent, CardStackQuantityChangeEvent>(OnStackQuantityChange);
        SubscribeLocalEvent<CardHandComponent, GetVerbsEvent<AlternativeVerb>>(OnAlternativeVerb);
    }

    private void OnStackQuantityChange(EntityUid uid, CardHandComponent comp, CardStackQuantityChangeEvent args)
    {
        if (_net.IsClient)
            return;

        if (!TryComp(uid, out CardStackComponent? stack))
            return;

        if (stack.Cards.Count < 0)
        {
            Log.Warning($"Invalid negative card count {stack.Cards.Count} detected in stack {ToPrettyString(uid)}");
            return;
        }

        var text = args.Type switch
        {
            StackQuantityChangeType.Added => "cards-stackquantitychange-added",
            StackQuantityChangeType.Removed => "cards-stackquantitychange-removed",
            StackQuantityChangeType.Joined => "cards-stackquantitychange-joined",
            StackQuantityChangeType.Split => "cards-stackquantitychange-split",
            _ => "cards-stackquantitychange-unknown"
        };

        _popupSystem.PopupEntity(Loc.GetString(text, ("quantity", stack.Cards.Count)), uid);

        _cardStack.FlipAllCards(uid, stack, comp.Flipped);
    }

    private void OnCardDraw(EntityUid uid, CardHandComponent comp, CardHandDrawMessage args)
    {
        if (!TryComp(uid, out CardStackComponent? stack))
            return;
        var pickup = _hands.IsHolding(args.Actor, uid);
        EntityUid? leftover = null;
        var cardEnt = GetEntity(args.Card);

        if (stack.Cards.Count == 2 && pickup)
        {
            leftover = stack.Cards[0] != cardEnt ? stack.Cards[0] : stack.Cards[1];
        }
        if (!_cardStack.TryRemoveCard(uid, cardEnt, stack))
            return;

        if (_net.IsServer)
            _storage.PlayPickupAnimation(cardEnt, Transform(cardEnt).Coordinates, Transform(args.Actor).Coordinates, 0);

        _hands.TryPickupAnyHand(args.Actor, cardEnt);
        if (pickup && leftover != null)
        {
            _hands.TryPickupAnyHand(args.Actor, leftover.Value);
        }
    }

    private void OpenHandMenu(EntityUid user, EntityUid hand)
    {
        if (!TryComp<ActorComponent>(user, out var actor))
            return;

        _ui.OpenUi(hand, CardUiKey.Key, actor.PlayerSession);

    }

    private void OnAlternativeVerb(EntityUid uid, CardHandComponent comp, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || args.Hands == null)
            return;

        args.Verbs.Add(new AlternativeVerb()
        {
            Act = () => OpenHandMenu(args.User, uid),
            Text = Loc.GetString("cards-verb-pickcard"),
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/die.svg.192dpi.png")),
            Priority = 4
        });
        args.Verbs.Add(new AlternativeVerb()
        {
            Act = () => _cardStack.ShuffleCards(uid),
            Text = Loc.GetString("cards-verb-shuffle"),
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/die.svg.192dpi.png")),
            Priority = 3
        });
        args.Verbs.Add(new AlternativeVerb()
        {
            Act = () => FlipCards(uid, comp),
            Text = Loc.GetString("cards-verb-flip"),
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/flip.svg.192dpi.png")),
            Priority = 2
        });
        args.Verbs.Add(new AlternativeVerb()
        {
            Act = () => ConvertToDeck(args.User, uid),
            Text = Loc.GetString("cards-verb-convert-to-deck"),
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/rotate_cw.svg.192dpi.png")),
            Priority = 1
        });
    }

    private void OnInteractUsing(EntityUid uid, CardComponent comp, InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (HasComp<CardStackComponent>(args.Used) ||
                !TryComp(args.Used, out CardComponent? usedComp))
            return;

        if (!HasComp<CardStackComponent>(args.Target) &&
                TryComp(args.Target, out CardComponent? targetCardComp))
        {
            TrySetupHandOfCards(args.User, args.Used, usedComp, args.Target, targetCardComp, true);
            args.Handled = true;
        }
    }

    private void ConvertToDeck(EntityUid user, EntityUid hand)
    {
        if (_net.IsClient)
            return;

        var cardDeck = SpawnInSameParent(CardDeckBaseName, hand);
        bool isHoldingCards = _hands.IsHolding(user, hand);

        EnsureComp<CardStackComponent>(cardDeck, out var deckStack);
        if (!TryComp(hand, out CardStackComponent? handStack))
            return;
        _cardStack.TryJoinStacks(cardDeck, hand, deckStack, handStack, null);

        if (isHoldingCards)
            _hands.TryPickupAnyHand(user, cardDeck);
    }
    public void TrySetupHandOfCards(EntityUid user, EntityUid card, CardComponent comp, EntityUid target, CardComponent targetComp, bool pickup)
    {
        if (card == target || _net.IsClient)
            return;
        var cardHand = SpawnInSameParent(CardHandBaseName, card);
        if (TryComp<CardHandComponent>(cardHand, out var handComp))
            handComp.Flipped = targetComp.Flipped;
        if (!TryComp(cardHand, out CardStackComponent? stack))
            return;
        if (!_cardStack.TryInsertCard(cardHand, card, stack) || !_cardStack.TryInsertCard(cardHand, target, stack))
            return;
        if (_net.IsServer)
            _storage.PlayPickupAnimation(card, Transform(card).Coordinates, Transform(cardHand).Coordinates, 0);
        if (pickup && !_hands.TryPickupAnyHand(user, cardHand))
            return;
        _cardStack.FlipAllCards(cardHand, stack, targetComp.Flipped);
    }

    public void TrySetupHandFromStack(EntityUid user, EntityUid card, CardComponent comp, EntityUid target, CardStackComponent targetComp, bool pickup)
    {
        if (_net.IsClient)
            return;
        var cardHand = SpawnInSameParent(CardHandBaseName, card);
        if (TryComp<CardHandComponent>(cardHand, out var handComp))
            handComp.Flipped = comp.Flipped;
        if (!TryComp(cardHand, out CardStackComponent? stack))
            return;
        if (!_cardStack.TryInsertCard(cardHand, card, stack))
            return;
        _cardStack.TransferNLastCardFromStacks(user, 1, target, targetComp, cardHand, stack);
        if (pickup && !_hands.TryPickupAnyHand(user, cardHand))
            return;
        _cardStack.FlipAllCards(cardHand, stack, comp.Flipped);
    }

    private void FlipCards(EntityUid hand, CardHandComponent comp)
    {
        comp.Flipped = !comp.Flipped;
        _cardStack.FlipAllCards(hand, null, comp.Flipped);
    }

    // Frontier: tries to spawn an entity with the same parent as another given entity.
    //           Useful when spawning decks/hands in a backpack, for example.
    private EntityUid SpawnInSameParent(EntProtoId prototype, EntityUid uid)
    {
        if (prototype == default)
            throw new ArgumentException("Cannot spawn with null prototype", nameof(prototype));

        if (_container.IsEntityOrParentInContainer(uid) &&
            _container.TryGetOuterContainer(uid, Transform(uid), out var container))
        {
            var entity = SpawnInContainerOrDrop(prototype, container.Owner, container.ID);
            if (!Exists(entity))
                Log.Error($"Failed to spawn {prototype} in container {container.ID}");
            return entity;
        }
        var worldEntity = Spawn(prototype, Transform(uid).Coordinates);
        if (!Exists(worldEntity))
            Log.Error($"Failed to spawn {prototype} at coordinates {Transform(uid).Coordinates}");
        return worldEntity;
    }
}
