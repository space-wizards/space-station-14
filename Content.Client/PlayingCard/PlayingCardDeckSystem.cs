using Content.Shared.PlayingCard;
using Robust.Client.GameObjects;

namespace Content.Client.PlayingCard;

public sealed class PlayingCardDeckSystem : VisualizerSystem<PlayingCardDeckVisualsComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, PlayingCardDeckVisualsComponent component, ref AppearanceChangeEvent args)
    {
        if (TryComp(uid, out SpriteComponent? sprite))
        // && args.Component.TryGetData(PlayingCardDeckVisuals.FacingUp, out bool isFacingUp))
        {
            // sprite.LayerSetVisible(PlayingCardDeckVisualLayers.Base, false);
            // sprite.LayerSetVisible(PlayingCardDeckVisualLayers.Details, false);
            // sprite.LayerSetVisible(PlayingCardDeckVisualLayers.FlippedDown, !false);
        }
    }
}

public enum PlayingCardDeckVisualLayers
{
    Base
}
