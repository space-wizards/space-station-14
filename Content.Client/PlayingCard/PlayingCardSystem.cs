using Content.Shared.PlayingCard;
using Robust.Client.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Client.PlayingCard;

public sealed class PlayingCardSystem : VisualizerSystem<PlayingCardVisualsComponent>
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    protected override void OnAppearanceChange(EntityUid uid, PlayingCardVisualsComponent component, ref AppearanceChangeEvent args)
    {
        if (TryComp(uid, out SpriteComponent? sprite)
        && args.Component.TryGetData(PlayingCardVisuals.FacingUp, out bool isFacingUp)
        && args.Component.TryGetData(PlayingCardVisuals.CardName, out string CardName)
        && args.Component.TryGetData(PlayingCardVisuals.PlayingCardContentPrototypeID, out string playingCardContentPrototypeID))
        {

            sprite.LayerSetVisible(PlayingCardVisualLayers.LayerOne, false);
            sprite.LayerSetVisible(PlayingCardVisualLayers.LayerTwo, false);

            sprite.LayerSetVisible(PlayingCardVisualLayers.Base, isFacingUp);

            sprite.LayerSetVisible(PlayingCardVisualLayers.FlippedDown, !isFacingUp);

            if (string.IsNullOrEmpty(playingCardContentPrototypeID))
                return;

            if (!_prototypeManager.TryIndex(playingCardContentPrototypeID, out PlayingCardContentsPrototype? playingCardContents))
                return;

            if (!playingCardContents.CardContents.TryGetValue(CardName, out string? cardLayerDetailsPrototypeID))
                return;

            if (!_prototypeManager.TryIndex(cardLayerDetailsPrototypeID, out PlayingCardDetailsPrototype? cardDetails))
                return;

            if (cardDetails.LayerOneState != null)
            {
                sprite.LayerSetState(PlayingCardVisualLayers.LayerOne, cardDetails.LayerOneState);
                sprite.LayerSetVisible(PlayingCardVisualLayers.LayerOne, isFacingUp);
            }

            if (cardDetails.LayerOneColor != null)
                sprite.LayerSetColor(PlayingCardVisualLayers.LayerOne, cardDetails.LayerOneColor.Value);

            if (cardDetails.LayerTwoState != null)
            {
                sprite.LayerSetState(PlayingCardVisualLayers.LayerTwo, cardDetails.LayerTwoState);
                sprite.LayerSetVisible(PlayingCardVisualLayers.LayerTwo, isFacingUp);
            }

            if (cardDetails.LayerTwoColor != null)
                sprite.LayerSetColor(PlayingCardVisualLayers.LayerTwo, cardDetails.LayerTwoColor.Value);
        }
    }
}

public enum PlayingCardVisualLayers
{
    Base,
    LayerOne,
    LayerTwo,
    FlippedDown
}
