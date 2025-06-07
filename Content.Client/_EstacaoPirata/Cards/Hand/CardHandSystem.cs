using System.Linq;
using System.Numerics;
using Content.Shared._EstacaoPirata.Cards.Hand;
using Content.Shared._EstacaoPirata.Cards.Stack;
using Robust.Client.GameObjects;

namespace Content.Client._EstacaoPirata.Cards.Hand;

/// <summary>
/// This handles...
/// </summary>
public sealed class CardHandSystem : EntitySystem
{
    private readonly Dictionary<Entity<CardHandComponent>, int> _notInit = [];
    [Dependency] private readonly CardSpriteSystem _cardSpriteSystem = default!;


    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<CardHandComponent, ComponentStartup>(OnComponentStartupEvent);
        SubscribeNetworkEvent<CardStackInitiatedEvent>(OnStackStart);
        SubscribeNetworkEvent<CardStackQuantityChangeEvent>(OnStackUpdate);
        SubscribeNetworkEvent<CardStackReorderedEvent>(OnStackReorder);
        SubscribeNetworkEvent<CardStackFlippedEvent>(OnStackFlip);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        foreach (var (ent, value) in _notInit)
        {
            if (value >= 5)
            {
                _notInit.Remove(ent);
                continue;
            }
            _notInit[ent] = value + 1;
            if(!TryComp(ent.Owner, out CardStackComponent? stack) || stack.Cards.Count <= 0)
                continue;

            // If cards were correctly initialized, we update the sprite
            UpdateSprite(ent.Owner, ent.Comp);
            _notInit.Remove(ent);
        }
    }

    private bool TryGetCardLayer(EntityUid card, out SpriteComponent.Layer? layer)
    {
        layer = null;
        if (!TryComp(card, out SpriteComponent? cardSprite))
            return false;

        if (!cardSprite.TryGetLayer(0, out var l))
            return false;

        layer = l;
        return true;
    }

    private void UpdateSprite(EntityUid uid, CardHandComponent comp)
    {
        if (!TryComp(uid, out SpriteComponent? sprite))
            return;

        if (!TryComp(uid, out CardStackComponent? cardStack))
            return;

        // Prevents error appearing at spawnMenu
        if (cardStack.Cards.Count <= 0 || !TryGetCardLayer(cardStack.Cards.Last(), out var cardlayer) ||
            cardlayer == null)
        {
            _notInit[(uid, comp)] = 0;
            return;
        }

        _cardSpriteSystem.TryAdjustLayerQuantity((uid, sprite, cardStack), comp.CardLimit);

        var cardCount = Math.Min(cardStack.Cards.Count, comp.CardLimit);

        // Frontier: zero/one card case
        if (cardCount <= 0)
        {
            // Placeholder - we need to have a valid sprite.
            sprite.LayerSetVisible(0, true);
            sprite.LayerSetState(0, "singlecard_down_black");
            sprite.LayerSetOffset(0, new Vector2(0f, 0f));
            sprite.LayerSetScale(0, new Vector2(1f, 1f));
        }
        else if (cardCount == 1)
        {
            _cardSpriteSystem.TryHandleLayerConfiguration(
                (uid, sprite, cardStack),
                cardCount,
                (sprt, cardIndex, layerIndex) =>
                {
                    sprt.Comp.LayerSetRotation(layerIndex, Angle.FromDegrees(0));
                    sprt.Comp.LayerSetOffset(layerIndex, new Vector2(0, 0.10f));
                    sprt.Comp.LayerSetScale(layerIndex, new Vector2(comp.Scale, comp.Scale));
                    return true;
                }
            );
        }
        else
        {
            var intervalAngle = comp.Angle / (cardCount-1);
            var intervalSize = comp.XOffset / (cardCount - 1);

            _cardSpriteSystem.TryHandleLayerConfiguration(
                (uid, sprite, cardStack),
                cardCount,
                (sprt, cardIndex, layerIndex) =>
                {
                    var angle = (-(comp.Angle/2)) + cardIndex * intervalAngle;
                    var x = (-(comp.XOffset / 2)) + cardIndex * intervalSize;
                    var y = -(x * x) + 0.10f;

                    sprt.Comp.LayerSetRotation(layerIndex, Angle.FromDegrees(-angle));
                    sprt.Comp.LayerSetOffset(layerIndex, new Vector2(x, y));
                    sprt.Comp.LayerSetScale(layerIndex, new Vector2(comp.Scale, comp.Scale));
                    return true;
                }
            );
        }
    }


    private void OnStackUpdate(CardStackQuantityChangeEvent args)
    {
        if (!TryComp(GetEntity(args.Stack), out CardHandComponent? comp))
            return;
        UpdateSprite(GetEntity(args.Stack), comp);
    }

    private void OnStackStart(CardStackInitiatedEvent args)
    {
        var entity = GetEntity(args.CardStack);
        if (!TryComp(entity, out CardHandComponent? comp))
            return;

        UpdateSprite(entity, comp);
    }
    private void OnComponentStartupEvent(EntityUid uid, CardHandComponent comp, ComponentStartup args)
    {
        if (!TryComp(uid, out CardStackComponent? stack))
        {
            _notInit[(uid, comp)] = 0;
            return;
        }
        if(stack.Cards.Count <= 0)
            _notInit[(uid, comp)] = 0;
        UpdateSprite(uid, comp);
    }

    // Frontier
    private void OnStackReorder(CardStackReorderedEvent args)
    {
        if (!TryComp(GetEntity(args.Stack), out CardHandComponent? comp))
            return;
        UpdateSprite(GetEntity(args.Stack), comp);
    }

    private void OnStackFlip(CardStackFlippedEvent args)
    {
        var entity = GetEntity(args.CardStack);
        if (!TryComp(entity, out CardHandComponent? comp))
            return;

        UpdateSprite(entity, comp);
    }
}
