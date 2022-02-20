using Content.Shared.PlayingCard;
using Robust.Client.GameObjects;

namespace Content.Client.PlayingCard;

public sealed class PlayingCardSystem : VisualizerSystem<PlayingCardVisualsComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, PlayingCardVisualsComponent component, ref AppearanceChangeEvent args)
    {
        if (TryComp(uid, out SpriteComponent? sprite)
        && args.Component.TryGetData(PlayingCardVisuals.FacingUp, out bool isFacingUp))
        {
            sprite.LayerSetVisible(PlayingCardVisualLayers.Base, isFacingUp);
            sprite.LayerSetVisible(PlayingCardVisualLayers.Details, isFacingUp);
            sprite.LayerSetVisible(PlayingCardVisualLayers.FlippedDown, !isFacingUp);
        }
    }
}

public enum PlayingCardVisualLayers
{
    Base,
    Details,
    FlippedDown
}
