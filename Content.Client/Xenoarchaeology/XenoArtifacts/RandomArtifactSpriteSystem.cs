using Content.Shared.Xenoarchaeology.XenoArtifacts;
using Robust.Client.GameObjects;

namespace Content.Client.Xenoarchaeology.XenoArtifacts;

public sealed class RandomArtifactSpriteSystem : VisualizerSystem<RandomArtifactSpriteComponent>
{
    [Dependency] private readonly SpriteSystem _sprite = default!;

    protected override void OnAppearanceChange(EntityUid uid, RandomArtifactSpriteComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (!AppearanceSystem.TryGetData<int>(uid, SharedArtifactsVisuals.SpriteIndex, out var spriteIndex, args.Component))
            return;

        if (!AppearanceSystem.TryGetData<bool>(uid, SharedArtifactsVisuals.IsUnlocking, out var isUnlocking, args.Component))
            isUnlocking = false;

        if (!AppearanceSystem.TryGetData<bool>(uid, SharedArtifactsVisuals.IsActivated, out var isActivated, args.Component))
            isActivated = false;

        var spriteIndexStr = spriteIndex.ToString("D2");
        var spritePrefix = isUnlocking ? "_on" : "";

        // layered artifact sprite
        if (_sprite.LayerMapTryGet((uid, args.Sprite), ArtifactsVisualLayers.UnlockingEffect, out var layer, false))
        {
            var spriteState = "ano" + spriteIndexStr;
            _sprite.LayerSetRsiState((uid, args.Sprite), ArtifactsVisualLayers.Base, spriteState);
            _sprite.LayerSetRsiState((uid, args.Sprite), layer, spriteState + "_on");
            _sprite.LayerSetVisible((uid, args.Sprite), layer, isUnlocking);

            if (_sprite.LayerMapTryGet((uid, args.Sprite), ArtifactsVisualLayers.ActivationEffect, out var activationEffectLayer, false))
            {
                _sprite.LayerSetRsiState((uid, args.Sprite), activationEffectLayer, "artifact-activation");
                _sprite.LayerSetVisible((uid, args.Sprite), activationEffectLayer, isActivated);
            }
        }
        // non-layered
        else
        {
            var spriteState = "ano" + spriteIndexStr + spritePrefix;
            _sprite.LayerSetRsiState((uid, args.Sprite), ArtifactsVisualLayers.Base, spriteState);
        }
    }
}

public enum ArtifactsVisualLayers : byte
{
    Base,
    UnlockingEffect, // doesn't have to use this
    ActivationEffect
}
