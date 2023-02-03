using Content.Shared.Wall;
using Robust.Client.GameObjects;

namespace Content.Client.Wall;

public sealed class ReinforcedWallVisualizerSystem : VisualizerSystem<ReinforcedWallVisualizerComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, ReinforcedWallVisualizerComponent comp, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;
        if(!AppearanceSystem.TryGetData(uid, ReinforcedWallVisuals.DeconstructionStage, out int stage, args.Component))
            return;

        SetDeconstructionStage(uid, stage, comp, args.Component, args.Sprite);
    }

    public void SetDeconstructionStage(EntityUid uid, int stage, ReinforcedWallVisualizerComponent comp, AppearanceComponent component, SpriteComponent sprite)
    {
        var index = sprite.LayerMapReserveBlank(ReinforcedWallVisualLayers.Deconstruction);
        if (stage < 0)
        {
            sprite.LayerSetVisible(index, false);
            return;
        }

        sprite.LayerSetVisible(index, true);
        sprite.LayerSetState(index, $"reinf_construct-{stage}");
    }
}

public enum ReinforcedWallVisualLayers : byte
{
    Deconstruction,
}
