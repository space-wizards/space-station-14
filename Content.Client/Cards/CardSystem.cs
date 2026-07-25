using System.Numerics;
using Content.Shared.Cards;
using Content.Shared.Mobs.Components;
using Content.Shared.Stacks;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Player;

namespace Content.Client.Cards;

/// <inheritdoc />
[UsedImplicitly]
public sealed partial class CardSystem : SharedCardSystem
{
    [Dependency]
    private SpriteSystem _sprite = default!;

    [Dependency]
    private IPlayerManager _playerManager = default!;

    /// <summary>
    /// Calculates the local position of a card on a fanned arc, given its angle from center. It is shifted downwards 3/4 of radius to center it.
    /// </summary>
    /// <param name="angle">The angle of the card along the fan, in radians, where 0 is centered.</param>
    /// <param name="radius">The radius of the fan's arc.</param>
    /// <returns>The local position offset for the card. In pixel coordinates scale.</returns>
    public static Vector2 FanPosition(double angle, float radius) =>
        new((float)Math.Sin(angle) * radius, (float)Math.Cos(angle) * radius - radius * (3f / 4f));

    /// <summary>
    /// Calculates the radius of the fan arc based on the number of cards.
    /// </summary>
    /// <param name="count">The total number of cards in the fan.</param>
    /// <returns>The fan radius, or 0 if there is only one card, since a single card cannot be fanned.</returns>
    public static float FanRadius(int count) => count <= 1 ? 0f : (float)Math.Sqrt(count / 20f);

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
        float angle = (idx - count / 2.0f + 0.5f) / count * (float)Math.PI;
        var position = FanPosition(angle, radius);
        var rotation = new Angle(-angle);
        return (position, rotation);
    }

    /// <summary>
    /// Gets the layer names for the 3 layers sprite layers used for a card.
    /// </summary>
    /// <param name="idx">The index of the card of those to be fanned.</param>
    /// <returns>The three layer names</returns>
    private static (string Base, string LayerOne, string LayerTwo) CardLayers(int idx) =>
        ($"card_{idx}_base", $"card_{idx}_layerOne", $"card_{idx}_layerTwo");

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
            if (flipped)
                visualState.Start = 0;
            flipped = false;
        }

        // Delete all layers which are not used here
        // Assumes that all layers will have the card before it have a layer
        // If it runs into a layer which doesn't exists it assumes no more later layers will exists
        // Might run into problems if the MaxFanned changes frequently
        for (var i = visualState.Count; i < visualState.MaxFanned; i++)
        {
            var (baseLayer, layerOne, layerTwo) = CardLayers(i);
            if (!_sprite.LayerExists((uid, sprite), baseLayer))
                break;
            _sprite.RemoveLayer((uid, sprite), baseLayer);
            _sprite.RemoveLayer((uid, sprite), layerOne);
            _sprite.RemoveLayer((uid, sprite), layerTwo);
        }

        var radius = FanRadius(visualState.Count);
        for (var i = 0; i < visualState.Count; i++)
        {
            var card = visualState.CardList[visualState.Start + i];
            var (baseLayer, layerOne, layerTwo) = CardLayers(i);

            if (card.CardId.Id == null || !PrototypeManager.TryIndex(card.CardId, out var prototype))
                continue;

            var (position, rotation) = GetCardPosRot(i, visualState.Count, radius);

            // Creates layers if not already present.
            _sprite.LayerMapReserve((uid, sprite), baseLayer);
            _sprite.LayerMapReserve((uid, sprite), layerOne);
            _sprite.LayerMapReserve((uid, sprite), layerTwo);

            if (flipped)
            {
                // Creates card and moves
                BuildCard(prototype, baseLayer, card.BaseState, layerOne, layerTwo, (uid, sprite));
                TransformLayer(layerOne, position, rotation, (uid, sprite));
                TransformLayer(layerTwo, position, rotation, (uid, sprite));
            }
            else
            {
                // Uses the base layer for the back side
                BuildLayer(baseLayer, card.CardBack, null, (uid, sprite));
                _sprite.LayerSetVisible((uid, sprite), layerOne, false);
                _sprite.LayerSetVisible((uid, sprite), layerTwo, false);
            }
            // Moves the shared layer
            TransformLayer(baseLayer, position, rotation, (uid, sprite));

            // Moves the stack texture below the left most card
            if (i == 0)
                TransformLayer("base", position, rotation, (uid, sprite));
        }
    }

    // Adds sprites to sprite layers and colours them.
    private void BuildCard(
        CardPrototype prototype,
        string baseLayer,
        string baseSprite,
        string layerOne,
        string layerTwo,
        Entity<SpriteComponent?> sprite
    )
    {
        BuildLayer(baseLayer, baseSprite, null, sprite);
        BuildLayer(layerOne, prototype.LayerOneState, prototype.LayerOneColor, sprite);
        BuildLayer(layerTwo, prototype.LayerTwoState, prototype.LayerTwoColor, sprite);
    }

    private void BuildLayer(string layer, string? layerState, Color? layerColor, Entity<SpriteComponent?> sprite)
    {
        _sprite.LayerSetVisible(sprite, layer, true);
        _sprite.LayerSetRsiState(sprite, layer, layerState);
        if (layerColor != null)
            _sprite.LayerSetColor(sprite, layer, layerColor.Value);
    }

    private void TransformLayer(string layer, Vector2 movement, Angle rotation, Entity<SpriteComponent?> sprite)
    {
        _sprite.LayerSetOffset(sprite, layer, movement);
        _sprite.LayerSetRotation(sprite, layer, rotation);
    }
}
