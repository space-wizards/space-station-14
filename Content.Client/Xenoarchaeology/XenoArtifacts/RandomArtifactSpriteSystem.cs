using Content.Shared.Xenoarchaeology.XenoArtifacts;
using Robust.Client.GameObjects;

namespace Content.Client.Xenoarchaeology.XenoArtifacts;

public sealed class RandomArtifactSpriteSystem : VisualizerSystem<RandomArtifactSpriteComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, RandomArtifactSpriteComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (!AppearanceSystem.TryGetData(uid, SharedArtifactsVisuals.SpriteIndex, out var ind, args.Component)
            || ind is not int spriteIndex)
            return;

        if (!AppearanceSystem.TryGetData(uid, SharedArtifactsVisuals.IsActivated, out var act, args.Component)
            || act is not bool isActivated)
            isActivated = false;

        var spriteIndexStr = spriteIndex.ToString("D2");
        var spritePrefix = isActivated ? "_on" : "";

        // layered artifact sprite
        if (args.Sprite.LayerMapTryGet(ArtifactsVisualLayers.Effect, out var layer))
        {
            var spriteState = "ano" + spriteIndexStr;
            args.Sprite.LayerSetState(ArtifactsVisualLayers.Base, spriteState);
            args.Sprite.LayerSetState(layer, spriteState + "_on");
            args.Sprite.LayerSetVisible(layer, isActivated);
        }
        // non-layered
        else
        {
            var spriteState = "ano" + spriteIndexStr + spritePrefix;
            args.Sprite.LayerSetState(ArtifactsVisualLayers.Base, spriteState);
        }

    }
}

public enum ArtifactsVisualLayers : byte
{
    Base,
    Effect // doesn't have to use this
}
