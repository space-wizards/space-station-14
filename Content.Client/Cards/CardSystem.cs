using System.Linq;
using System.Numerics;
using Content.Shared.Cards;
using Content.Shared.Mobs.Components;
using Content.Shared.Stacks;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Shared.Utility;

namespace Content.Client.Cards;

/// <inheritdoc />
[UsedImplicitly]
public sealed partial class CardSystem : SharedCardSystem
{
    [Dependency] private SpriteSystem _sprite = default!;

    [Dependency] private IPlayerManager _playerManager = default!;

    [SubscribeLocalEvent]
    private void OnAppearanceChanged(EntityUid uid, CardsComponent _, ref AppearanceChangeEvent args)
    {
        Appearance.TryGetData<bool>(uid, CardVisuals.IsFlipped, out var flipped, args.Component);

        // Card visuals state will only have one card in it if not fanned
        // It will have a max of MaxFanned when fanned
        if (!Appearance.TryGetData<CardListVisualState>(uid, CardVisuals.CardList, out var visualState, args.Component))
            visualState = new CardListVisualState(new List<CardData>(), 0, 0, 1);

        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        var xform = Transform(uid);

        // Hide in strip menu
        // TODO: This should be done in a less bad way. The strip menu system should have a method or field for setting this.
        if (
            HasComp<MobStateComponent>(xform.ParentUid)
            && xform.ParentUid != _playerManager.LocalSession?.AttachedEntity
        )
        {
            flipped = false;
        }

        // Delete all layers which are not used here
        // Assumes that all layers will have the card before it have a layer
        // If it runs into a layer which doesn't exists it assumes no more later layers will exists
        // Might run into problems if the MaxFanned changes frequently
        for (var i = visualState.Count; i < visualState.MaxFanned; i++)
        {
            var card = visualState.CardList[visualState.Start + (flipped ? i : -i - 1)];
            if (card.CardId.Id == null || !PrototypeManager.TryIndex(card.CardId, out var prototype))
                continue;
            var cardLayers = CardLayers(i, prototype.Layers.Count);
            if (!_sprite.LayerExists((uid, sprite), cardLayers[0]))
                break;
            foreach (var layer in cardLayers)
                _sprite.RemoveLayer((uid, sprite), layer, true);
        }

        var radius = FanRadius(visualState.Count);
        for (var i = 0; i < visualState.Count; i++)
        {
            // If flipped counts from the back
            var card = visualState.CardList[visualState.Start + (flipped ? i : -i - 1)];
            if (card.CardId.Id == null || !PrototypeManager.TryIndex(card.CardId, out var prototype))
                continue;
            Log.Info($"{prototype.Sprite}");
            Log.Info($"{prototype.Color}");
            var cardLayers = CardLayers(i, prototype.Layers.Count);

            var (position, rotation) = GetCardPosRot(i, visualState.Count, radius);

            foreach (var layer in cardLayers)
                _sprite.LayerMapReserve((uid, sprite), layer);

            if (flipped)
            {
                // Creates card and moves
                BuildCard(prototype, cardLayers, card.BaseState, (uid, sprite));
                foreach (var layer in cardLayers)
                    TransformLayer(layer, position, rotation, (uid, sprite));
            }
            else
            {
                // Uses the base layer for the back side
                BuildLayer(cardLayers[0], prototype.Sprite, card.CardBack, null, (uid, sprite));
                TransformLayer(cardLayers[0], position, rotation, (uid, sprite));
                foreach (var layer in cardLayers)
                {
                    _sprite.LayerSetVisible((uid, sprite), layer, false);
                }
                _sprite.LayerSetVisible((uid, sprite), cardLayers[0], true);
            }
            // Moves the stack texture below the left most card
            if (i == 0)
                TransformLayer("base", position, rotation, (uid, sprite));
        }
    }

    /// <summary>
    /// Calculates the local position of a card on a fanned arc, given its angle from center. It is shifted downwards 3/4 of radius to center it.
    /// </summary>
    /// <param name="angle">The angle of the card along the fan, in radians, where 0 is centered.</param>
    /// <param name="radius">The radius of the fan's arc.</param>
    /// <returns>The local position offset for the card. In pixel coordinates scale.</returns>
    public static Vector2 FanPosition(float angle, float radius) =>
        new(MathF.Sin(angle) * radius, MathF.Cos(angle) * radius - radius * (3f / 4f));

    /// <summary>
    /// Calculates the radius of the fan arc based on the number of cards.
    /// </summary>
    /// <param name="count">The total number of cards in the fan.</param>
    /// <returns>The fan radius, or 0 if there is only one card, since a single card cannot be fanned.</returns>
    public static float FanRadius(int count) => count <= 1 ? 0f : MathF.Sqrt(count / 20f);

    /// <summary>
    /// Calculates the position and rotation of a card at a given index within a fanned hand,
    /// arranging cards in a semi-circle from left to right.
    /// </summary>
    /// <param name="idx">The index of the card of those to be fanned.</param>
    /// <param name="count">The total number of cards in the hand.</param>
    /// <returns>A tuple containing the card's position and rotation.</returns>
    public static (Vector2, Angle) GetCardPosRot(int idx, int count)
    {
        var radius = FanRadius(count);
        return GetCardPosRot(idx, count, radius);
    }

    /// <summary>
    /// Calculates the position and rotation of a card at a given index within a fanned hand,
    /// arranging cards in a semi-circle from left to right.
    /// </summary>
    /// <param name="idx">The index of the card of those to be fanned.</param>
    /// <param name="count">The total number of cards in the hand.</param>
    /// <param name="radius">The radius of the fan's arc.</param>
    /// <returns>A tuple containing the card's position and rotation.</returns>
    public static (Vector2, Angle) GetCardPosRot(int idx, int count, float radius)
    {
        // Semi-circle from left to right
        float angle = (idx - count / 2.0f + 0.5f) / count * MathF.PI;
        var position = FanPosition(angle, radius);
        var rotation = new Angle(-angle);
        return (position, rotation);
    }

    /// <summary>
    /// Gets the layer names for the 3 layers sprite layers used for a card.
    /// </summary>
    /// <param name="idx">The index of the card of those to be fanned.</param>
    /// <returns>The three layer names</returns>
    private static List<string> CardLayers(int index, int layerCount)
    {
        List<string> list = new();
        list.Add($"card_{index}_base");
        for (var i = 0; i < layerCount; i++)
        {
            list.Add($"card_{index}_{layerCount}");
        }
        return list;
    }

    // Adds sprites to sprite layers and colours them.
    private void BuildCard(
        CardPrototype prototype,
        List<string> cardLayers,
        string baseSprite,
        Entity<SpriteComponent?> sprite
    )
    {
        BuildLayer(cardLayers[0], prototype.Sprite, baseSprite, null, sprite);
        var i = 1;
        foreach (var card in prototype.Layers)
        {
            BuildLayer(cardLayers[i], prototype.Sprite, card.State, card.Color ?? prototype.Color, sprite);
            i++;
        }
    }

    private void BuildLayer(string layer, string rsi, string layerState, Color? layerColor, Entity<SpriteComponent?> sprite)
    {
        _sprite.LayerSetVisible(sprite, layer, true);
        _sprite.LayerSetSprite(sprite, layer, new SpriteSpecifier.Rsi(new ResPath(rsi), layerState));
        if (layerColor != null)
            _sprite.LayerSetColor(sprite, layer, layerColor.Value);
    }

    private void TransformLayer(string layer, Vector2 movement, Angle rotation, Entity<SpriteComponent?> sprite)
    {
        _sprite.LayerSetOffset(sprite, layer, movement);
        _sprite.LayerSetRotation(sprite, layer, rotation);
    }
}
