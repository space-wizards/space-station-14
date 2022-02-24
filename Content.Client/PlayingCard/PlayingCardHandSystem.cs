using Content.Shared.PlayingCard;
using Robust.Client.GameObjects;

namespace Content.Client.PlayingCard;

public sealed class PlayingCardHandSystem : VisualizerSystem<PlayingCardHandVisualsComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, PlayingCardHandVisualsComponent component, ref AppearanceChangeEvent args)
    {
        if (TryComp(uid, out SpriteComponent? sprite)
        && args.Component.TryGetData(PlayingCardHandVisuals.CardCount, out int cardCount))
        {
            if (cardCount == 2)
            {
                sprite.LayerSetVisible(PlayingCardHandVisualLayers.ThirdCardDetails, false);
                // sprite.LayerSetVisible(PlayingCardHandVisualLayers.ThirdCardBase, false);
                sprite.LayerSetVisible(PlayingCardHandVisualLayers.FourthCardDetails, false);
                // sprite.LayerSetVisible(PlayingCardHandVisualLayers.FourthCardBase, false);
                // sprite.LayerSetVisible(PlayingCardHandVisualLayers.ManyCardHandBase, false);

                sprite.LayerSetOffset(PlayingCardHandVisualLayers.FirstCardDetails, new Vector2(-.2f,0f));
                sprite.LayerSetRotation(PlayingCardHandVisualLayers.FirstCardDetails, new Angle(0.523599));
                sprite.LayerSetOffset(PlayingCardHandVisualLayers.SecondCardDetails, new Vector2(.2f,0f));
                sprite.LayerSetRotation(PlayingCardHandVisualLayers.SecondCardDetails, new Angle(5.93412));

                // sprite.LayerSetState(PlayingCardHandVisualLayers.FirstCardDetails, cardList[0]);
                // sprite.LayerSetState(PlayingCardHandVisualLayers.SecondCardDetails, cardList[1]);

            }
            if (cardCount == 3)
            {

                sprite.LayerSetVisible(PlayingCardHandVisualLayers.ThirdCardDetails, true);
                // sprite.LayerSetVisible(PlayingCardHandVisualLayers.ThirdCardBase, false);
                // sprite.LayerSetVisible(PlayingCardHandVisualLayers.FourthCardDetails, false);
                // sprite.LayerSetVisible(PlayingCardHandVisualLayers.FourthCardBase, false);
                // sprite.LayerSetVisible(PlayingCardHandVisualLayers.ManyCardHandBase, false);

                sprite.LayerSetOffset(PlayingCardHandVisualLayers.FirstCardDetails, new Vector2(-.3f,0f));
                sprite.LayerSetRotation(PlayingCardHandVisualLayers.FirstCardDetails, new Angle(0.349066));
                // sprite.LayerSetOffset(PlayingCardHandVisualLayers.SecondCardDetails, new Vector2(.2f,0f));
                // sprite.LayerSetRotation(PlayingCardHandVisualLayers.SecondCardDetails, new Angle(5.93412));
                sprite.LayerSetOffset(PlayingCardHandVisualLayers.ThirdCardDetails, new Vector2(.3f,0f));
                sprite.LayerSetRotation(PlayingCardHandVisualLayers.ThirdCardDetails, new Angle(6.10865));

                // sprite.LayerSetState(PlayingCardHandVisualLayers.FirstCardDetails, cardList[0]);
                // sprite.LayerSetState(PlayingCardHandVisualLayers.SecondCardDetails, cardList[1]);
                // sprite.LayerSetState(PlayingCardHandVisualLayers.ThirdCardDetails, cardList[2]);
            }
            if (cardCount == 4)
            {
                sprite.LayerSetVisible(PlayingCardHandVisualLayers.ThirdCardDetails, true);
                sprite.LayerSetVisible(PlayingCardHandVisualLayers.FourthCardDetails, true);

                sprite.LayerSetOffset(PlayingCardHandVisualLayers.FirstCardDetails, new Vector2(-.2f,-.1f));
                sprite.LayerSetOffset(PlayingCardHandVisualLayers.SecondCardDetails, new Vector2(-.05f,0f));
                sprite.LayerSetOffset(PlayingCardHandVisualLayers.ThirdCardDetails, new Vector2(.1f,.1f));
                sprite.LayerSetOffset(PlayingCardHandVisualLayers.FourthCardDetails, new Vector2(.25f,.15f));


                // sprite.LayerSetState(PlayingCardHandVisualLayers.FirstCardDetails, cardList[0]);
                // sprite.LayerSetState(PlayingCardHandVisualLayers.SecondCardDetails, cardList[1]);
                // sprite.LayerSetState(PlayingCardHandVisualLayers.ThirdCardDetails, cardList[2]);
                // sprite.LayerSetState(PlayingCardHandVisualLayers.FourthCardDetails, cardList[3]);
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

                // sprite.LayerSetState(PlayingCardHandVisualLayers.FirstCardDetails, cardList[0]);
                // sprite.LayerSetState(PlayingCardHandVisualLayers.SecondCardDetails, cardList[1]);
                // sprite.LayerSetState(PlayingCardHandVisualLayers.ThirdCardDetails, cardList[2]);
                // sprite.LayerSetState(PlayingCardHandVisualLayers.FourthCardDetails, cardList[3]);
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
