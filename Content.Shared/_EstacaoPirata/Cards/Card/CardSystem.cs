using Content.Shared._EstacaoPirata.Cards.Deck;
using Content.Shared._EstacaoPirata.Cards.Hand;
using Content.Shared._EstacaoPirata.Cards.Stack;
using Content.Shared.Examine;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Verbs;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._EstacaoPirata.Cards.Card;

/// <summary>
/// This handles...
/// </summary>
public sealed class CardSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly CardStackSystem _cardStack = default!;
    [Dependency] private readonly CardDeckSystem _cardDeck = default!;
    [Dependency] private readonly CardHandSystem _cardHand = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<CardComponent, GetVerbsEvent<AlternativeVerb>>(AddTurnOnVerb);
        SubscribeLocalEvent<CardComponent, GetVerbsEvent<ActivationVerb>>(OnActivationVerb);
        SubscribeLocalEvent<CardComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<CardComponent, UseInHandEvent>(OnUse);
        SubscribeLocalEvent<CardComponent, ActivateInWorldEvent>(OnActivate);
    }
    private void OnExamined(EntityUid uid, CardComponent component, ExaminedEvent args)
    {
        if (args.IsInDetailsRange && !component.Flipped)
        {
            args.PushMarkup(Loc.GetString("card-examined", ("target",  Loc.GetString(component.Name))));
        }
    }

    private void AddTurnOnVerb(EntityUid uid, CardComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || args.Hands == null)
            return;

        args.Verbs.Add(new AlternativeVerb()
        {
            Act = () => FlipCard(uid, component),
            Text = Loc.GetString("cards-verb-flip"),
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/flip.svg.192dpi.png")),
            Priority = 1
        });

        if (args.Using == null || args.Using == args.Target)
            return;

        if (TryComp<CardStackComponent>(args.Using, out var usingStack))
        {
            args.Verbs.Add(new AlternativeVerb()
            {
                Act = () => JoinCards(args.User, args.Target, component, (EntityUid)args.Using, usingStack),
                Text = Loc.GetString("card-verb-join"),
                Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/refresh.svg.192dpi.png")),
                Priority = 2
            });
        }
        else if (TryComp<CardComponent>(args.Using, out var usingCard))
        {
            args.Verbs.Add(new AlternativeVerb()
            {
                Act = () => _cardHand.TrySetupHandOfCards(args.User, args.Target, component, args.Using.Value, usingCard, false),
                Text = Loc.GetString("card-verb-join"),
                Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/refresh.svg.192dpi.png")),
                Priority = 2
            });
        }
    }

    private void OnUse(EntityUid uid, CardComponent comp, UseInHandEvent args)
    {
        if (args.Handled)
            return;

        FlipCard(uid, comp);
        args.Handled = true;
    }

    /// <summary>
    /// Server-Side only method to flip card. This starts CardFlipUpdatedEvent event
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="component"></param>
    private void FlipCard(EntityUid uid, CardComponent component)
    {
        if (_net.IsClient)
            return;
        component.Flipped = !component.Flipped;
        Dirty(uid, component);
        RaiseNetworkEvent(new CardFlipUpdatedEvent(GetNetEntity(uid)));
    }

    private void JoinCards(EntityUid user, EntityUid first, CardComponent firstComp, EntityUid second, CardStackComponent secondStack)
    {
        if (_net.IsClient)
            return;

        EntityUid cardStack;
        bool? flip = null;
        if (HasComp<CardDeckComponent>(second))
        {
            cardStack = SpawnInSameParent(_cardDeck.CardDeckBaseName, first);
        }
        else if (HasComp<CardHandComponent>(second))
        {
            cardStack = SpawnInSameParent(_cardHand.CardHandBaseName, first);
            if(TryComp<CardHandComponent>(cardStack, out var stackHand))
                stackHand.Flipped = firstComp.Flipped;
            flip = firstComp.Flipped;
        }
        else
            return;

        if (!TryComp(cardStack, out CardStackComponent? stack))
            return;
        if (!_cardStack.TryInsertCard(cardStack, first, stack))
            return;
        _cardStack.TransferNLastCardFromStacks(user, secondStack.Cards.Count, second, secondStack, cardStack, stack);
        if (flip != null)
            _cardStack.FlipAllCards(cardStack, stack, flip); //???
    }

    // Frontier: tries to spawn an entity with the same parent as another given entity.
    //           Useful when spawning decks/hands in a backpack, for example.
    private EntityUid SpawnInSameParent(EntProtoId prototype, EntityUid uid)
    {
        if (_container.IsEntityOrParentInContainer(uid) &&
            _container.TryGetOuterContainer(uid, Transform(uid), out var container))
        {
            return SpawnInContainerOrDrop(prototype, container.Owner, container.ID);
        }
        return Spawn(prototype, Transform(uid).Coordinates);
    }

    // Frontier: hacky misuse of the activation verb, but allows us a separate way to draw cards without needing additional buttons and event fiddling
    private void OnActivationVerb(EntityUid uid, CardComponent component, GetVerbsEvent<ActivationVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || args.Hands == null)
            return;

        if (args.Using == args.Target)
            return;

        if (HasComp<CardStackComponent>(uid))
            return;

        if (args.Using == null)
        {
            args.Verbs.Add(new ActivationVerb()
            {
                Act = () => _hands.TryPickupAnyHand(args.User, args.Target),
                Text = Loc.GetString("cards-verb-draw"),
                Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/eject.svg.192dpi.png")),
                Priority = 16
            });
        }
        else if (TryComp<CardStackComponent>(args.Using, out var cardStack))
        {
            args.Verbs.Add(new ActivationVerb()
            {
                Act = () => _cardStack.InsertCardOnStack(args.User, args.Using.Value, cardStack, args.Target),
                Text = Loc.GetString("cards-verb-draw"),
                Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/eject.svg.192dpi.png")),
                Priority = 16
            });
        }
        else if (TryComp<CardComponent>(args.Using, out var card))
        {
            args.Verbs.Add(new ActivationVerb()
            {
                Act = () => _cardHand.TrySetupHandOfCards(args.User, args.Using.Value, card, args.Target, component, true),
                Text = Loc.GetString("cards-verb-draw"),
                Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/eject.svg.192dpi.png")),
                Priority = 16
            });
        }
    }
    // End Frontier

    private void OnActivate(EntityUid uid, CardComponent component, ActivateInWorldEvent args)
    {
        if (!args.Complex || args.Handled)
            return;

        if (!TryComp<HandsComponent>(args.User, out var hands))
            return;

        // Card stacks are handled differently
        if (HasComp<CardStackComponent>(args.Target))
            return;

        var activeItem = _hands.GetActiveItem((args.User, hands));

        if (activeItem == null)
        {
            _hands.TryPickupAnyHand(args.User, args.Target);
        }
        else if (TryComp<CardStackComponent>(activeItem, out var cardStack))
        {
            _cardStack.InsertCardOnStack(args.User, activeItem.Value, cardStack, args.Target);
        }
        else if (TryComp<CardComponent>(activeItem, out var card))
        {
            _cardHand.TrySetupHandOfCards(args.User, activeItem.Value, card, args.Target, component, true);
        }
    }
}
