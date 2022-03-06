using Content.Shared.PlayingCard;
using Robust.Client.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Client.PlayingCard;

public sealed class PlayingCardHandSystem : VisualizerSystem<PlayingCardHandVisualsComponent>
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    protected override void OnAppearanceChange(EntityUid uid, PlayingCardHandVisualsComponent component, ref AppearanceChangeEvent args)
    {
        // if (TryComp(uid, out SpriteComponent? sprite)
        // && args.Component.TryGetData(PlayingCardHandVisuals.CardList, out CardListVisualState visualState))
        // {
        //     List<string> cardList = visualState.CardList;
        //     int cardCount = cardList.Count;

        //     string playingCardContent = visualState.PlayingCardContentPrototypeID;


        //     if (string.IsNullOrEmpty(playingCardContent))
        //         return;

        //     if (!_prototypeManager.TryIndex(playingCardContent, out PlayingCardContentsPrototype? playingCardContents))
        //         return;


        //     sprite.LayerSetVisible(PlayingCardHandVisualLayers.ManyCardHandBase, false);
        //     sprite.LayerSetVisible(PlayingCardHandVisualLayers.FourthCardBase, false);
        //     sprite.LayerSetVisible(PlayingCardHandVisualLayers.FourthCardLayerOne, false);
        //     sprite.LayerSetVisible(PlayingCardHandVisualLayers.FourthCardLayerTwo, false);
        //     sprite.LayerSetVisible(PlayingCardHandVisualLayers.ThirdCardBase, false);
        //     sprite.LayerSetVisible(PlayingCardHandVisualLayers.ThirdCardLayerOne, false);
        //     sprite.LayerSetVisible(PlayingCardHandVisualLayers.ThirdCardLayerTwo, false);
        //     sprite.LayerSetVisible(PlayingCardHandVisualLayers.SecondCardLayerOne, false);
        //     sprite.LayerSetVisible(PlayingCardHandVisualLayers.SecondCardLayerTwo, false);
        //     sprite.LayerSetVisible(PlayingCardHandVisualLayers.FirstCardLayerOne, false);
        //     sprite.LayerSetVisible(PlayingCardHandVisualLayers.FirstCardLayerTwo, false);
        //     // first and second cards should always be in use in a hand

        //     if (cardCount == 2)
        //     {
        //         sprite.LayerSetOffset(PlayingCardHandVisualLayers.FirstCardBase, new Vector2(-.2f,0f));
        //         sprite.LayerSetRotation(PlayingCardHandVisualLayers.FirstCardBase, new Angle(0.523599));
        //         sprite.LayerSetOffset(PlayingCardHandVisualLayers.FirstCardLayerOne, new Vector2(-.2f,0f));
        //         sprite.LayerSetRotation(PlayingCardHandVisualLayers.FirstCardLayerOne, new Angle(0.523599));
        //         sprite.LayerSetOffset(PlayingCardHandVisualLayers.FirstCardLayerTwo, new Vector2(-.2f,0f));
        //         sprite.LayerSetRotation(PlayingCardHandVisualLayers.FirstCardLayerTwo, new Angle(0.523599));

        //         sprite.LayerSetOffset(PlayingCardHandVisualLayers.SecondCardBase, new Vector2(.2f,0f));
        //         sprite.LayerSetRotation(PlayingCardHandVisualLayers.SecondCardBase, new Angle(5.93412));
        //         sprite.LayerSetOffset(PlayingCardHandVisualLayers.SecondCardLayerOne, new Vector2(.2f,0f));
        //         sprite.LayerSetRotation(PlayingCardHandVisualLayers.SecondCardLayerOne, new Angle(5.93412));
        //         sprite.LayerSetOffset(PlayingCardHandVisualLayers.SecondCardLayerTwo, new Vector2(.2f,0f));
        //         sprite.LayerSetRotation(PlayingCardHandVisualLayers.SecondCardLayerTwo, new Angle(5.93412));

        //         sprite.LayerSetVisible(PlayingCardHandVisualLayers.FirstCardLayerTwo, true);

        //         if (playingCardContents.CardContents.TryGetValue(cardList[1], out string? cardLayerDetailsPrototypeID))
        //         {
        //             if (_prototypeManager.TryIndex(cardLayerDetailsPrototypeID, out PlayingCardDetailsPrototype? cardDetails))
        //             {
        //                 if (cardDetails.LayerOneState != null)
        //                 {
        //                     sprite.LayerSetVisible(PlayingCardHandVisualLayers.FirstCardLayerOne, true);
        //                     sprite.LayerSetState(PlayingCardHandVisualLayers.FirstCardLayerOne, cardDetails.LayerOneState);
        //                     if (cardDetails.LayerOneColor != null)
        //                     {
        //                         sprite.LayerSetColor(PlayingCardHandVisualLayers.FirstCardLayerOne, cardDetails.LayerOneColor.Value);
        //                     }
        //                 }
        //                 if (cardDetails.LayerTwoState != null)
        //                 {
        //                     sprite.LayerSetVisible(PlayingCardHandVisualLayers.FirstCardLayerTwo, true);
        //                     sprite.LayerSetState(PlayingCardHandVisualLayers.FirstCardLayerTwo, cardDetails.LayerTwoState);
        //                     if (cardDetails.LayerOneColor != null)
        //                     {
        //                         sprite.LayerSetColor(PlayingCardHandVisualLayers.FirstCardLayerTwo, cardDetails.LayerOneColor.Value);
        //                     }
        //                 }
        //             }
        //         }



        //         sprite.LayerSetState(PlayingCardHandVisualLayers.FirstCardDetails, cardList[1]);
        //         sprite.LayerSetState(PlayingCardHandVisualLayers.FirstCardDetails, cardList[1]);
        //         sprite.LayerSetState(PlayingCardHandVisualLayers.FirstCardDetails, cardList[1]);


        //         sprite.LayerSetState(PlayingCardHandVisualLayers.FirstCardDetails, cardList[1]);
        //         sprite.LayerSetState(PlayingCardHandVisualLayers.FirstCardDetails, cardList[1]);
        //         sprite.LayerSetState(PlayingCardHandVisualLayers.FirstCardDetails, cardList[1]);



        //         sprite.LayerSetState(PlayingCardHandVisualLayers.SecondCardDetails, cardList[0]);
        //         return;
        //     }
        //     if (cardCount == 3)
        //     {
        //         sprite.LayerSetVisible(PlayingCardHandVisualLayers.ThirdCardDetails, true);

        //         sprite.LayerSetOffset(PlayingCardHandVisualLayers.FirstCardDetails, new Vector2());
        //         sprite.LayerSetRotation(PlayingCardHandVisualLayers.FirstCardDetails, new Angle());
        //         sprite.LayerSetOffset(PlayingCardHandVisualLayers.SecondCardDetails, new Vector2());
        //         sprite.LayerSetRotation(PlayingCardHandVisualLayers.SecondCardDetails, new Angle());

        //         sprite.LayerSetOffset(PlayingCardHandVisualLayers.FirstCardDetails, new Vector2(-.3f,0f));
        //         sprite.LayerSetRotation(PlayingCardHandVisualLayers.FirstCardDetails, new Angle(0.349066));
        //         sprite.LayerSetOffset(PlayingCardHandVisualLayers.ThirdCardDetails, new Vector2(.3f,0f));
        //         sprite.LayerSetRotation(PlayingCardHandVisualLayers.ThirdCardDetails, new Angle(6.10865));

        //         if (!noUniqueCards)
        //         {
        //             sprite.LayerSetState(PlayingCardHandVisualLayers.FirstCardDetails, cardList[2]);
        //             sprite.LayerSetState(PlayingCardHandVisualLayers.SecondCardDetails, cardList[1]);
        //             sprite.LayerSetState(PlayingCardHandVisualLayers.ThirdCardDetails, cardList[0]);
        //         }
        //         return;
        //     }
        //     if (cardCount == 4)
        //     {
        //         sprite.LayerSetVisible(PlayingCardHandVisualLayers.ThirdCardDetails, true);
        //         sprite.LayerSetVisible(PlayingCardHandVisualLayers.FourthCardDetails, true);

        //         sprite.LayerSetOffset(PlayingCardHandVisualLayers.FirstCardDetails, new Vector2(-.2f,-.1f));
        //         sprite.LayerSetOffset(PlayingCardHandVisualLayers.SecondCardDetails, new Vector2(-.05f,0f));
        //         sprite.LayerSetOffset(PlayingCardHandVisualLayers.ThirdCardDetails, new Vector2(.1f,.1f));
        //         sprite.LayerSetOffset(PlayingCardHandVisualLayers.FourthCardDetails, new Vector2(.25f,.15f));

        //         if (!noUniqueCards)
        //         {
        //             sprite.LayerSetState(PlayingCardHandVisualLayers.FirstCardDetails, cardList[3]);
        //             sprite.LayerSetState(PlayingCardHandVisualLayers.SecondCardDetails, cardList[2]);
        //             sprite.LayerSetState(PlayingCardHandVisualLayers.ThirdCardDetails, cardList[1]);
        //             sprite.LayerSetState(PlayingCardHandVisualLayers.FourthCardDetails, cardList[0]);
        //         }
        //         return;
        //     }
        //     if (cardCount > 4)
        //     {
        //         sprite.LayerSetVisible(PlayingCardHandVisualLayers.ManyCardHandBase, true);
        //         sprite.LayerSetVisible(PlayingCardHandVisualLayers.ThirdCardDetails, true);
        //         sprite.LayerSetVisible(PlayingCardHandVisualLayers.FourthCardDetails, true);

        //         sprite.LayerSetOffset(PlayingCardHandVisualLayers.FirstCardDetails, new Vector2(-.2f,-.1f));
        //         sprite.LayerSetOffset(PlayingCardHandVisualLayers.SecondCardDetails, new Vector2(-.05f,0f));
        //         sprite.LayerSetOffset(PlayingCardHandVisualLayers.ThirdCardDetails, new Vector2(.1f,.1f));
        //         sprite.LayerSetOffset(PlayingCardHandVisualLayers.FourthCardDetails, new Vector2(.25f,.15f));

        //         if (!noUniqueCards)
        //         {
        //             sprite.LayerSetState(PlayingCardHandVisualLayers.FirstCardDetails, cardList[cardList.Count - 1]);
        //             sprite.LayerSetState(PlayingCardHandVisualLayers.SecondCardDetails, cardList[cardList.Count - 2]);
        //             sprite.LayerSetState(PlayingCardHandVisualLayers.ThirdCardDetails, cardList[cardList.Count - 3]);
        //             sprite.LayerSetState(PlayingCardHandVisualLayers.FourthCardDetails, cardList[cardList.Count - 4]);
        //         }
        //         return;
        //     }
        // }
    }
    // public void BuildCard (string cardName, SpriteComponent sprite, PlayingCardContentsPrototype contentsPrototype)
    // {
    //     if (contentsPrototype.CardContents.TryGetValue(cardName, out string? cardLayerDetailsPrototypeID))
    //     {
    //         if (_prototypeManager.TryIndex(cardLayerDetailsPrototypeID, out PlayingCardDetailsPrototype? cardDetails))
    //         {
    //             if (cardDetails.LayerOneState != null)
    //             {
    //                 sprite.LayerSetVisible(PlayingCardHandVisualLayers.FirstCardLayerOne, true);
    //                 sprite.LayerSetState(PlayingCardHandVisualLayers.FirstCardLayerOne, cardDetails.LayerOneState);
    //                 if (cardDetails.LayerOneColor != null)
    //                 {
    //                     sprite.LayerSetColor(PlayingCardHandVisualLayers.FirstCardLayerOne, cardDetails.LayerOneColor.Value);
    //                 }
    //             }
    //             if (cardDetails.LayerTwoState != null)
    //             {
    //                 sprite.LayerSetVisible(PlayingCardHandVisualLayers.FirstCardLayerTwo, true);
    //                 sprite.LayerSetState(PlayingCardHandVisualLayers.FirstCardLayerTwo, cardDetails.LayerTwoState);
    //                 if (cardDetails.LayerOneColor != null)
    //                 {
    //                     sprite.LayerSetColor(PlayingCardHandVisualLayers.FirstCardLayerTwo, cardDetails.LayerOneColor.Value);
    //                 }
    //             }
    //         }
    //     }
    // }
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
