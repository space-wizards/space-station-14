using System.Linq;
using Content.Shared._EstacaoPirata.Cards.Card;
using Content.Shared._EstacaoPirata.Cards.Deck;
using Content.Shared._EstacaoPirata.Cards.Hand;
using Content.Shared.Examine;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Storage.EntitySystems;
using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Shared._EstacaoPirata.Cards.Stack;

/// <summary>
/// This handles stack of cards.
/// It is used to shuffle, flip, insert, remove, and join stacks of cards.
/// It also handles the events related to the stack of cards.
/// </summary>
public sealed class CardStackSystem : EntitySystem
{
    public const string ContainerId = "cardstack-container";
    public const int MaxCardsInStack = 212; // Frontier: four 53-card decks.

    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly EntityManager _entityManager = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedStorageSystem _storage = default!;
    [Dependency] private readonly CardHandSystem _cardHandSystem = default!; // Frontier
    [Dependency] private readonly SharedHandsSystem _hands = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        // Pretty much a rip-off of the BinSystem
        SubscribeLocalEvent<CardStackComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<CardStackComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<CardStackComponent, EntRemovedFromContainerMessage>(OnEntRemoved);
        SubscribeLocalEvent<CardStackComponent, GetVerbsEvent<AlternativeVerb>>(OnAlternativeVerb);
        SubscribeLocalEvent<CardStackComponent, GetVerbsEvent<ActivationVerb>>(OnActivationVerb);
        SubscribeLocalEvent<CardStackComponent, ActivateInWorldEvent>(OnActivate);
        SubscribeLocalEvent<CardStackComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<InteractUsingEvent>(OnInteractUsing);
    }

    public bool TryRemoveCard(EntityUid uid, EntityUid card, CardStackComponent? comp = null)
    {
        if (!Resolve(uid, ref comp))
            return false;

        if (!TryComp(card, out CardComponent? _))
            return false;

        _container.Remove(card, comp.ItemContainer);
        comp.Cards.Remove(card);

        // If there is a final card left over, remove that card from the container and delete the stack alltogether
        if (comp.Cards.Count == 1)
        {

            _container.Remove(comp.Cards.First(), comp.ItemContainer);
            comp.Cards.Clear();
        }

        Dirty(uid, comp);

        RaiseLocalEvent(uid, new CardStackQuantityChangeEvent(GetNetEntity(uid), GetNetEntity(card), StackQuantityChangeType.Removed));
        RaiseNetworkEvent(new CardStackQuantityChangeEvent(GetNetEntity(uid), GetNetEntity(card), StackQuantityChangeType.Removed));
        // Prevents prediction ruining things
        if (_net.IsServer && comp.Cards.Count <= 0)
        {
            _entityManager.DeleteEntity(uid);
        }
        return true;
    }

    public bool TryInsertCard(EntityUid uid, EntityUid card, CardStackComponent? comp = null)
    {
        if (!Resolve(uid, ref comp))
            return false;

        if (!TryComp(card, out CardComponent? _))
            return false;

        if (comp.Cards.Count >= MaxCardsInStack)
            return false;

        _container.Insert(card, comp.ItemContainer);
        comp.Cards.Add(card);

        Dirty(uid, comp);
        RaiseLocalEvent(uid, new CardStackQuantityChangeEvent(GetNetEntity(uid), GetNetEntity(card), StackQuantityChangeType.Added));
        RaiseNetworkEvent(new CardStackQuantityChangeEvent(GetNetEntity(uid), GetNetEntity(card), StackQuantityChangeType.Added));
        return true;
    }

    public bool ShuffleCards(EntityUid uid, CardStackComponent? comp = null)
    {
        if (!Resolve(uid, ref comp))
            return false;

        _random.Shuffle(comp.Cards);

        Dirty(uid, comp);
        RaiseLocalEvent(uid, new CardStackReorderedEvent(GetNetEntity(uid)));
        RaiseNetworkEvent(new CardStackReorderedEvent(GetNetEntity(uid)));
        return true;
    }

    /// <summary>
    /// Server-Side only method to flip all cards within a stack. This starts CardFlipUpdatedEvent and CardStackFlippedEvent event
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="comp"></param>
    /// <param name="isFlipped">If null, all cards will just invert direction, if it contains a value, then all cards will receive that value</param>
    /// <returns></returns>
    public bool FlipAllCards(EntityUid uid, CardStackComponent? comp = null, bool? isFlipped = null)
    {
        if (_net.IsClient)
            return false;
        if (!Resolve(uid, ref comp))
            return false;
        foreach (var card in comp.Cards)
        {
            if (!TryComp(card, out CardComponent? cardComponent))
                continue;

            cardComponent.Flipped = isFlipped ?? !cardComponent.Flipped;

            Dirty(card, cardComponent);
            RaiseNetworkEvent(new CardFlipUpdatedEvent(GetNetEntity(card)));
        }

        RaiseNetworkEvent(new CardStackFlippedEvent(GetNetEntity(uid)));
        return true;
    }

    public bool TryJoinStacks(EntityUid firstStack, EntityUid secondStack, CardStackComponent? firstComp = null, CardStackComponent? secondComp = null, EntityUid? soundUser = null)
    {
        if (firstStack == secondStack)
            return false;
        if (!Resolve(firstStack, ref firstComp) || !Resolve(secondStack, ref secondComp))
            return false;

        bool changed = false;
        var cardList = secondComp.Cards.ToList();
        EntityUid? firstCard = secondComp.Cards.Count > 0 ? cardList[0] : null; // Cache the first card transferred for animations (better to have something moving than nothing, and we destroy the other stack)

        foreach (var card in cardList)
        {
            if (firstComp.Cards.Count >= MaxCardsInStack)
                break;
            _container.Remove(card, secondComp.ItemContainer);
            secondComp.Cards.Remove(card);
            firstComp.Cards.Add(card);
            _container.Insert(card, firstComp.ItemContainer);
            changed = true;
        }
        if (changed)
        {
            if (soundUser != null)
            {
                _audio.PlayPredicted(firstComp.PlaceDownSound, Transform(firstStack).Coordinates, soundUser.Value);
                if(_net.IsServer)
                    _storage.PlayPickupAnimation(firstCard!.Value, Transform(secondStack).Coordinates, Transform(firstStack).Coordinates, 0);
            }

            if (_net.IsClient)
                return changed;

            Dirty(firstStack, firstComp);
            if (secondComp.Cards.Count <= 0)
            {
                _entityManager.DeleteEntity(secondStack);
            }
            else
            {
                Dirty(secondStack, secondComp);
                RaiseLocalEvent(secondStack, new CardStackQuantityChangeEvent(GetNetEntity(secondStack), null, StackQuantityChangeType.Split));
                RaiseNetworkEvent(new CardStackQuantityChangeEvent(GetNetEntity(secondStack), null, StackQuantityChangeType.Split));
            }
            RaiseLocalEvent(firstStack, new CardStackQuantityChangeEvent(GetNetEntity(firstStack), null, StackQuantityChangeType.Joined));
            RaiseNetworkEvent(new CardStackQuantityChangeEvent(GetNetEntity(firstStack), null, StackQuantityChangeType.Joined));
        }

        return changed;
    }

    #region EventHandling

    private void OnStartup(EntityUid uid, CardStackComponent component, ComponentStartup args)
    {
        component.ItemContainer = _container.EnsureContainer<Container>(uid, ContainerId);
    }

    private void OnMapInit(EntityUid uid, CardStackComponent comp, MapInitEvent args)
    {
        if (_net.IsClient)
            return;

        var coordinates = Transform(uid).Coordinates;
        var spawnedEntities = new List<EntityUid>();
        foreach (var id in comp.InitialContent)
        {
            var ent = Spawn(id, coordinates);
            spawnedEntities.Add(ent);
            if (TryInsertCard(uid, ent, comp))
                continue;
            Log.Error($"Entity {ToPrettyString(ent)} was unable to be initialized into stack {ToPrettyString(uid)}");
            foreach (var spawned in spawnedEntities)
                _entityManager.DeleteEntity(spawned);
            return;
        }
        RaiseNetworkEvent(new CardStackInitiatedEvent(GetNetEntity(uid)));
    }

    // It seems the cards don't get removed if this event is not subscribed... strange right? thanks again bin system
    private void OnEntRemoved(EntityUid uid, CardStackComponent component, EntRemovedFromContainerMessage args)
    {
        component.Cards.Remove(args.Entity);
    }

    private void OnExamine(EntityUid uid, CardStackComponent component, ExaminedEvent args)
    {
        args.PushText(Loc.GetString("card-stack-examine", ("count", component.Cards.Count)));
    }

    private void OnAlternativeVerb(EntityUid uid, CardStackComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        if (args.Using == args.Target)
            return;
        if (!TryComp(args.Target, out CardStackComponent? targetStack))
            return;

        if (TryComp(args.Using, out CardStackComponent? usingStack))
        {
            args.Verbs.Add(new AlternativeVerb()
            {
                Text = Loc.GetString("card-verb-join"),
                Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/refresh.svg.192dpi.png")),
                Priority = 8,
                Act = () => JoinStacks(args.User, args.Target, targetStack, (EntityUid)args.Using, usingStack)
            });
        }
        else if (TryComp(args.Using, out CardComponent? usingCard)) // Frontier: single card interaction
        {
            args.Verbs.Add(new AlternativeVerb()
            {
                Text = Loc.GetString("card-verb-join"),
                Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/refresh.svg.192dpi.png")),
                Priority = 8,
                Act = () => InsertCardOnStack(args.User, args.Target, targetStack, (EntityUid)args.Using)
            });
        } // End Frontier: single card interaction
    }

    // Frontier: hacky misuse of the activation verb, but allows us a separate way to draw cards without needing additional buttons and event fiddling
    private void OnActivationVerb(EntityUid uid, CardStackComponent component, GetVerbsEvent<ActivationVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || args.Hands == null)
            return;

        if (args.Using == args.Target)
            return;

        if (args.Using == null)
        {
            args.Verbs.Add(new ActivationVerb()
            {
                Act = () => OnInteractHand(args.Target, component, args.User),
                Text = Loc.GetString("cards-verb-draw"),
                Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/eject.svg.192dpi.png")),
                Priority = 16
            });
        }
        else if (TryComp<CardStackComponent>(args.Using, out var cardStack))
        {
            args.Verbs.Add(new ActivationVerb()
            {
                Act = () => TransferNLastCardFromStacks(args.User, 1, args.Target, component, args.Using.Value, cardStack),
                Text = Loc.GetString("cards-verb-draw"),
                Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/eject.svg.192dpi.png")),
                Priority = 16
            });
        }
        else if (TryComp<CardComponent>(args.Using, out var card))
        {
            args.Verbs.Add(new ActivationVerb()
            {
                Act = () => _cardHandSystem.TrySetupHandFromStack(args.User, args.Using.Value, card, args.Target, component, true),
                Text = Loc.GetString("cards-verb-draw"),
                Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/eject.svg.192dpi.png")),
                Priority = 16
            });
        }
    }
    // End Frontier

    private void JoinStacks(EntityUid user, EntityUid first, CardStackComponent firstComp, EntityUid second, CardStackComponent secondComp)
    {
            TryJoinStacks(first, second, firstComp, secondComp, user);
    }

    public void InsertCardOnStack(EntityUid user, EntityUid stack, CardStackComponent stackComponent, EntityUid card)
    {
        if (!TryInsertCard(stack, card))
            return;

        _audio.PlayPredicted(stackComponent.PlaceDownSound, Transform(stack).Coordinates, user);
        if (_net.IsClient)
            return;
        _storage.PlayPickupAnimation(card, Transform(user).Coordinates, Transform(stack).Coordinates, 0);
    }

    /// <summary>
    /// This takes the last card from the first stack and inserts it into the second stack
    /// </summary>
    public void TransferNLastCardFromStacks(EntityUid user, int n, EntityUid first, CardStackComponent firstComp, EntityUid second, CardStackComponent secondComp)
    {
        if (firstComp.Cards.Count <= 0)
            return;

        var cards = firstComp.Cards.TakeLast(n).ToList(); // Frontier: make a copy we don't munge during iteration

        var firstCard = cards.First(); // Cache first card for animation - enumerable changes in foreach

        bool changed = false;
        foreach (var card in cards)
        {
            if (secondComp.Cards.Count >= MaxCardsInStack)
                break;
            _container.Remove(card, firstComp.ItemContainer);
            firstComp.Cards.Remove(card);
            secondComp.Cards.Add(card);
            _container.Insert(card, secondComp.ItemContainer);
            changed = true;
        }

        if (changed)
        {
            _audio.PlayPredicted(firstComp.PlaceDownSound, Transform(second).Coordinates, user);
            if (_net.IsClient)
                return;

            _storage.PlayPickupAnimation(firstCard, Transform(first).Coordinates, Transform(second).Coordinates, 0);

            Dirty(second, secondComp);
            if (firstComp.Cards.Count == 1)
            {
                var card = firstComp.Cards.First();
                _container.Remove(card, firstComp.ItemContainer);
                if (_hands.IsHolding(user, first))
                {
                    _hands.TryDrop(user, first);
                    _hands.TryPickupAnyHand(user, card);
                }
                firstComp.Cards.Clear();
            }
            if (firstComp.Cards.Count <= 0)
            {
                _entityManager.DeleteEntity(first);
            }
            else
            {
                Dirty(first, firstComp);
                RaiseLocalEvent(first, new CardStackQuantityChangeEvent(GetNetEntity(first), null, StackQuantityChangeType.Removed));
                RaiseNetworkEvent(new CardStackQuantityChangeEvent(GetNetEntity(first), null, StackQuantityChangeType.Removed));
            }
            RaiseLocalEvent(second, new CardStackQuantityChangeEvent(GetNetEntity(second), null, StackQuantityChangeType.Added));
            RaiseNetworkEvent(new CardStackQuantityChangeEvent(GetNetEntity(second), null, StackQuantityChangeType.Added));
        }
    }

    private void OnInteractUsing(InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (args.Target == args.Used)
            return;

        // This checks if the user is using an item with Stack component
        if (TryComp(args.Used, out CardStackComponent? usedStack))
        {
            // If the target is a card, then it will insert the card into the stack
            if (TryComp(args.Target, out CardComponent? _))
            {
                InsertCardOnStack(args.User, args.Used, usedStack, args.Target);
                args.Handled = true;
                return;
            }

            // If instead, the target is a stack, then it will join the two stacks
            if (!TryComp(args.Target, out CardStackComponent? targetStack))
                return;

            TransferNLastCardFromStacks(args.User, 1, args.Target, targetStack, args.Used, usedStack);
            args.Handled = true;
        }

        // This handles the reverse case, where the user is using a card and inserting it to a stack
        else if (TryComp(args.Target, out CardStackComponent? stack))
        {
            //InsertCardOnStack(args.User, args.Target, stack, args.Used); // Frontier: old version
            if (TryComp(args.Used, out CardComponent? card))
            {
                _cardHandSystem.TrySetupHandFromStack(args.User, args.Used, card, args.Target, stack, true);
                args.Handled = true;
            }
        }
    }

    private void OnInteractHand(EntityUid uid, CardStackComponent component, EntityUid user)
    {
        var pickup = _hands.IsHolding(user, uid);
        if (component.Cards.Count <= 0)
            return;

        if (!component.Cards.TryGetValue(component.Cards.Count - 1, out var card))
            return;
        if (!component.Cards.TryGetValue(component.Cards.Count - 2, out var under))
            return;

        if (!TryRemoveCard(uid, card, component))
            return;

        _hands.TryPickupAnyHand(user, card);
        if (!Exists(uid) && pickup)
            _hands.TryPickupAnyHand(user, under);

        if (TryComp<CardDeckComponent>(uid, out var deck))
            _audio.PlayPredicted(deck.PickUpSound, Transform(card).Coordinates, user);
        else
            _audio.PlayPredicted(component.PickUpSound, Transform(card).Coordinates, user);
    }

    private void OnActivate(EntityUid uid, CardStackComponent component, ActivateInWorldEvent args)
    {
        if (!args.Complex || args.Handled)
            return;

        if (!TryComp<HandsComponent>(args.User, out var hands))
        {
            args.Handled = true;
            return;
        }

        var activeItem = _hands.GetActiveItem((args.User, hands));

        if (activeItem == null)
        {
            // Runs if active item is nothing
            // behavior is to draw one card from this target onto active hand as a standalone card
            OnInteractHand(args.Target, component, args.User);
        }
        else if (activeItem == args.Target)
        {
            // Added from a Frontier PR. Don't want to draw a card from a stack onto itself.
            args.Handled = true;
            return;
        }
        else if (TryComp<CardStackComponent>(activeItem, out var cardStack))
        {
            // If the active item contains a card stack, behavior is to draw from Target and place onto activeHand.
            TransferNLastCardFromStacks(args.User, 1, args.Target, component, activeItem.Value, cardStack);
        }
        else if (TryComp<CardComponent>(activeItem, out var card))
        {
            _cardHandSystem.TrySetupHandFromStack(args.User, activeItem.Value, card, args.Target, component, true);
        }
        args.Handled = true;
    }

    #endregion
}
