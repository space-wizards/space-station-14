using Content.Shared.PlayingCard;
using Robust.Client.GameObjects;

namespace Content.Client.PlayingCard;

public sealed class PlayingCardHandSystem : VisualizerSystem<PlayingCardHandVisualsComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, PlayingCardHandVisualsComponent component, ref AppearanceChangeEvent args)
    {
        if (TryComp(uid, out SpriteComponent? sprite)
        && args.Component.TryGetData(PlayingCardHandVisuals.CardList, out CardListVisualState visualState))
        {
            List<string> cardList = visualState.CardList;
            int cardCount = cardList.Count;
            bool noUniqueCards = visualState.NoUniqueCardLayers;
            sprite.LayerSetVisible(PlayingCardHandVisualLayers.ManyCardHandBase, false);
            sprite.LayerSetVisible(PlayingCardHandVisualLayers.FourthCardDetails, false);
            sprite.LayerSetVisible(PlayingCardHandVisualLayers.ThirdCardDetails, false);
            // first and second cards should always be in use in a hand

            if (cardCount == 2)
            {
                sprite.LayerSetOffset(PlayingCardHandVisualLayers.FirstCardDetails, new Vector2(-.2f,0f));
                sprite.LayerSetRotation(PlayingCardHandVisualLayers.FirstCardDetails, new Angle(0.523599));
                sprite.LayerSetOffset(PlayingCardHandVisualLayers.SecondCardDetails, new Vector2(.2f,0f));
                sprite.LayerSetRotation(PlayingCardHandVisualLayers.SecondCardDetails, new Angle(5.93412));

                if (!noUniqueCards)
                {
                    sprite.LayerSetState(PlayingCardHandVisualLayers.FirstCardDetails, cardList[1]);
                    sprite.LayerSetState(PlayingCardHandVisualLayers.SecondCardDetails, cardList[0]);
                }
                return;
            }
            if (cardCount == 3)
            {
                sprite.LayerSetVisible(PlayingCardHandVisualLayers.ThirdCardDetails, true);

                sprite.LayerSetOffset(PlayingCardHandVisualLayers.FirstCardDetails, new Vector2());
                sprite.LayerSetRotation(PlayingCardHandVisualLayers.FirstCardDetails, new Angle());
                sprite.LayerSetOffset(PlayingCardHandVisualLayers.SecondCardDetails, new Vector2());
                sprite.LayerSetRotation(PlayingCardHandVisualLayers.SecondCardDetails, new Angle());

                sprite.LayerSetOffset(PlayingCardHandVisualLayers.FirstCardDetails, new Vector2(-.3f,0f));
                sprite.LayerSetRotation(PlayingCardHandVisualLayers.FirstCardDetails, new Angle(0.349066));
                sprite.LayerSetOffset(PlayingCardHandVisualLayers.ThirdCardDetails, new Vector2(.3f,0f));
                sprite.LayerSetRotation(PlayingCardHandVisualLayers.ThirdCardDetails, new Angle(6.10865));

                if (!noUniqueCards)
                {
                    sprite.LayerSetState(PlayingCardHandVisualLayers.FirstCardDetails, cardList[2]);
                    sprite.LayerSetState(PlayingCardHandVisualLayers.SecondCardDetails, cardList[1]);
                    sprite.LayerSetState(PlayingCardHandVisualLayers.ThirdCardDetails, cardList[0]);
                }
                return;
            }
            if (cardCount == 4)
            {
                sprite.LayerSetVisible(PlayingCardHandVisualLayers.ThirdCardDetails, true);
                sprite.LayerSetVisible(PlayingCardHandVisualLayers.FourthCardDetails, true);

                sprite.LayerSetOffset(PlayingCardHandVisualLayers.FirstCardDetails, new Vector2(-.2f,-.1f));
                sprite.LayerSetOffset(PlayingCardHandVisualLayers.SecondCardDetails, new Vector2(-.05f,0f));
                sprite.LayerSetOffset(PlayingCardHandVisualLayers.ThirdCardDetails, new Vector2(.1f,.1f));
                sprite.LayerSetOffset(PlayingCardHandVisualLayers.FourthCardDetails, new Vector2(.25f,.15f));

                if (!noUniqueCards)
                {
                    sprite.LayerSetState(PlayingCardHandVisualLayers.FirstCardDetails, cardList[3]);
                    sprite.LayerSetState(PlayingCardHandVisualLayers.SecondCardDetails, cardList[2]);
                    sprite.LayerSetState(PlayingCardHandVisualLayers.ThirdCardDetails, cardList[1]);
                    sprite.LayerSetState(PlayingCardHandVisualLayers.FourthCardDetails, cardList[0]);
                }
                return;
            }
            if (cardCount > 4)
            {
                sprite.LayerSetVisible(PlayingCardHandVisualLayers.ManyCardHandBase, true);
                sprite.LayerSetVisible(PlayingCardHandVisualLayers.ThirdCardDetails, true);
                sprite.LayerSetVisible(PlayingCardHandVisualLayers.FourthCardDetails, true);

                sprite.LayerSetOffset(PlayingCardHandVisualLayers.FirstCardDetails, new Vector2(-.2f,-.1f));
                sprite.LayerSetOffset(PlayingCardHandVisualLayers.SecondCardDetails, new Vector2(-.05f,0f));
                sprite.LayerSetOffset(PlayingCardHandVisualLayers.ThirdCardDetails, new Vector2(.1f,.1f));
                sprite.LayerSetOffset(PlayingCardHandVisualLayers.FourthCardDetails, new Vector2(.25f,.15f));

                if (!noUniqueCards)
                {
                    sprite.LayerSetState(PlayingCardHandVisualLayers.FirstCardDetails, cardList[cardList.Count - 1]);
                    sprite.LayerSetState(PlayingCardHandVisualLayers.SecondCardDetails, cardList[cardList.Count - 2]);
                    sprite.LayerSetState(PlayingCardHandVisualLayers.ThirdCardDetails, cardList[cardList.Count - 3]);
                    sprite.LayerSetState(PlayingCardHandVisualLayers.FourthCardDetails, cardList[cardList.Count - 4]);
                }
                return;
            }
        }
    }
}

public enum PlayingCardHandVisualLayers
{
    ManyCardHandBase,
    FourthCardBase,
    FourthCardDetails,
    ThirdCardBase,
    ThirdCardDetails,
    SecondCardBase,
    SecondCardDetails,
    FirstCardBase,
    FirstCardDetails,
}
