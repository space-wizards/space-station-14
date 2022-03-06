using Content.Shared.PlayingCard;
using Robust.Client.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Client.PlayingCard;

public sealed class PlayingCardHandSystem : VisualizerSystem<PlayingCardHandVisualsComponent>
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    protected override void OnAppearanceChange(EntityUid uid, PlayingCardHandVisualsComponent component, ref AppearanceChangeEvent args)
    {
        if (TryComp(uid, out SpriteComponent? sprite)
        && args.Component.TryGetData(PlayingCardHandVisuals.CardList, out CardListVisualState visualState))
        {
            List<string> cardList = visualState.CardList;
            int cardCount = cardList.Count;

            string playingCardContent = visualState.PlayingCardContentPrototypeID;


            if (string.IsNullOrEmpty(playingCardContent))
                return;

            if (!_prototypeManager.TryIndex(playingCardContent, out PlayingCardContentsPrototype? playingCardContents))
                return;

            sprite.LayerSetVisible(PlayingCardHandVisualLayers.ManyCardHandBase, false);
            sprite.LayerSetVisible(PlayingCardHandVisualLayers.FourthCardBase, false);
            sprite.LayerSetVisible(PlayingCardHandVisualLayers.FourthCardLayerOne, false);
            sprite.LayerSetVisible(PlayingCardHandVisualLayers.FourthCardLayerTwo, false);
            sprite.LayerSetVisible(PlayingCardHandVisualLayers.ThirdCardBase, false);
            sprite.LayerSetVisible(PlayingCardHandVisualLayers.ThirdCardLayerOne, false);
            sprite.LayerSetVisible(PlayingCardHandVisualLayers.ThirdCardLayerTwo, false);
            sprite.LayerSetVisible(PlayingCardHandVisualLayers.SecondCardLayerOne, false);
            sprite.LayerSetVisible(PlayingCardHandVisualLayers.SecondCardLayerTwo, false);
            sprite.LayerSetVisible(PlayingCardHandVisualLayers.FirstCardLayerOne, false);
            sprite.LayerSetVisible(PlayingCardHandVisualLayers.FirstCardLayerTwo, false);

            if (cardCount == 2)
            {
                TransformCard(10, 11, 12, new Vector2(-.2f,0f), new Angle(0.523599), sprite);
                TransformCard(7, 8, 9, new Vector2(.2f,0f), new Angle(5.93412), sprite);

                BuildCard(cardList[1], 10, 11, 12, sprite, playingCardContents);
                BuildCard(cardList[0], 7, 8, 9, sprite, playingCardContents);
                return;
            }
            if (cardCount == 3)
            {
                // reset card transform
                TransformCard(7, 8, 9, new Vector2(), new Angle(), sprite);

                TransformCard(10, 11, 12, new Vector2(-.3f,0f), new Angle(0.349066), sprite);
                TransformCard(4, 5, 6, new Vector2(.3f,0f), new Angle(6.10865), sprite);

                BuildCard(cardList[2], 10, 11, 12, sprite, playingCardContents);
                BuildCard(cardList[1], 7, 8, 9, sprite, playingCardContents);
                BuildCard(cardList[0], 4, 5, 6, sprite, playingCardContents);
                return;
            }
            if (cardCount == 4)
            {
                TransformCard(10, 11, 12, new Vector2(-.2f,-.1f), new Angle(), sprite);
                TransformCard(7, 8, 9, new Vector2(-.05f,0f), new Angle(), sprite);
                TransformCard(4, 5, 6, new Vector2(.1f,.1f), new Angle(), sprite);
                TransformCard(1, 2, 3, new Vector2(.25f,.15f), new Angle(), sprite);

                BuildCard(cardList[3], 10, 11, 12, sprite, playingCardContents);
                BuildCard(cardList[2], 7, 8, 9, sprite, playingCardContents);
                BuildCard(cardList[1], 4, 5, 6, sprite, playingCardContents);
                BuildCard(cardList[0], 1, 2, 3, sprite, playingCardContents);
                return;
            }
            if (cardCount > 4)
            {
                sprite.LayerSetVisible(PlayingCardHandVisualLayers.ManyCardHandBase, true);
                TransformCard(10, 11, 12, new Vector2(-.2f,-.1f), new Angle(), sprite);
                TransformCard(7, 8, 9, new Vector2(-.05f,0f), new Angle(), sprite);
                TransformCard(4, 5, 6, new Vector2(.1f,.1f), new Angle(), sprite);
                TransformCard(1, 2, 3, new Vector2(.25f,.15f), new Angle(), sprite);

                BuildCard(cardList[cardList.Count - 1], 10, 11, 12, sprite, playingCardContents);
                BuildCard(cardList[cardList.Count - 2], 7, 8, 9, sprite, playingCardContents);
                BuildCard(cardList[cardList.Count - 3], 4, 5, 6, sprite, playingCardContents);
                BuildCard(cardList[cardList.Count - 4], 1, 2, 3, sprite, playingCardContents);
                return;
            }
        }
    }
    public void BuildCard (string cardName, int baseLayer, int layerOne, int layerTwo, SpriteComponent sprite, PlayingCardContentsPrototype contentsPrototype)
    {
        sprite.LayerSetVisible(baseLayer, true);
        if (contentsPrototype.CardContents.TryGetValue(cardName, out string? cardLayerDetailsPrototypeID))
        {
            if (_prototypeManager.TryIndex(cardLayerDetailsPrototypeID, out PlayingCardDetailsPrototype? cardDetails))
            {
                if (cardDetails.LayerOneState != null)
                {
                    sprite.LayerSetVisible(layerOne, true);
                    sprite.LayerSetState(layerOne, cardDetails.LayerOneState);
                    if (cardDetails.LayerOneColor != null)
                    {
                        sprite.LayerSetColor(layerOne, cardDetails.LayerOneColor.Value);
                    }
                }
                if (cardDetails.LayerTwoState != null)
                {
                    sprite.LayerSetVisible(layerTwo, true);
                    sprite.LayerSetState(layerTwo, cardDetails.LayerTwoState);
                    if (cardDetails.LayerTwoColor != null)
                    {
                        sprite.LayerSetColor(layerTwo, cardDetails.LayerTwoColor.Value);
                    }
                }
            }
        }
    }

    public void TransformCard (int baseLayer, int layerOne, int layerTwo, Vector2 movement, Angle rotation, SpriteComponent sprite)
    {
        sprite.LayerSetOffset(baseLayer, movement);
        sprite.LayerSetRotation(baseLayer, rotation);

        sprite.LayerSetOffset(layerOne, movement);
        sprite.LayerSetRotation(layerOne, rotation);

        sprite.LayerSetOffset(layerTwo, movement);
        sprite.LayerSetRotation(layerTwo, rotation);
    }
}

public enum PlayingCardHandVisualLayers
{
    ManyCardHandBase,
    FourthCardBase,
    FourthCardLayerOne,
    FourthCardLayerTwo,
    ThirdCardBase,
    ThirdCardLayerOne,
    ThirdCardLayerTwo,
    SecondCardBase,
    SecondCardLayerOne,
    SecondCardLayerTwo,
    FirstCardBase,
    FirstCardLayerOne,
    FirstCardLayerTwo,
}
