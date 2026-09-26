using Content.Shared.Xenoarchaeology.XenoArtifacts;
using Robust.Client.GameObjects;

namespace Content.Client.Xenoarchaeology.XenoArtifacts;

public sealed partial class RandomArtifactSpriteSystem : VisualizerSystem<RandomArtifactSpriteComponent>
{
    /// <inheritdoc/>
    protected override void OnAppearanceChange(EntityUid uid, RandomArtifactSpriteComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (!args.TryGetData<int>(SharedArtifactsVisuals.SpriteIndex, out var spriteIndex))
            return;

        if (!args.TryGetData<bool>(SharedArtifactsVisuals.IsUnlocking, out var isUnlocking))
            isUnlocking = false;

        if (!args.TryGetData<bool>(SharedArtifactsVisuals.IsActivated, out var isActivated))
            isActivated = false;

        var spriteIndexStr = spriteIndex.ToString("D2");
        var spritePrefix = isUnlocking ? "_on" : "";

        // layered artifact sprite
        if (SpriteSystem.LayerMapTryGet((uid, args.Sprite), ArtifactsVisualLayers.UnlockingEffect, out var layer, false))
        {
            var spriteState = "ano" + spriteIndexStr;
            SpriteSystem.LayerSetRsiState((uid, args.Sprite), ArtifactsVisualLayers.Base, spriteState);
            SpriteSystem.LayerSetRsiState((uid, args.Sprite), layer, spriteState + "_on");
            SpriteSystem.LayerSetVisible((uid, args.Sprite), layer, isUnlocking);

            if (SpriteSystem.LayerMapTryGet((uid, args.Sprite), ArtifactsVisualLayers.ActivationEffect, out var activationEffectLayer, false))
            {
                SpriteSystem.LayerSetRsiState((uid, args.Sprite), activationEffectLayer, "artifact-activation");
                SpriteSystem.LayerSetVisible((uid, args.Sprite), activationEffectLayer, isActivated);
            }
        }
        // non-layered
        else
        {
            var spriteState = "ano" + spriteIndexStr + spritePrefix;
            SpriteSystem.LayerSetRsiState((uid, args.Sprite), ArtifactsVisualLayers.Base, spriteState);
        }
    }
}

public enum ArtifactsVisualLayers : byte
{
    Base,
    UnlockingEffect, // doesn't have to use this
    ActivationEffect
}
