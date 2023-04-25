using Content.Shared.Wall;
using Robust.Client.GameObjects;

namespace Content.Client.Wall
{
    public sealed class ReinforcedWallVisualizerSystem : VisualizerSystem<ReinforcedWallVisualsComponent>
    {
        protected override void OnAppearanceChange(EntityUid uid, ReinforcedWallVisualsComponent component, ref AppearanceChangeEvent args)
        {
            if(args.Sprite == null)
                return;
            if (!args.AppearanceData.TryGetValue(ReinforcedWallVisuals.DeconstructionStage, out var deconStage))
                return;
            int stage = (int)deconStage;
            SetDeconstructionStage(stage, args.Sprite);
        }

        private void SetDeconstructionStage(int stage, SpriteComponent sprite)
        {
            object index = sprite.LayerMapReserveBlank(ReinforcedWallVisualsComponent.ReinforcedWallVisualLayers.Deconstruction);

            if (stage < 0)
            {
                sprite.LayerSetVisible(index, false);
                return;
            }

            sprite.LayerSetVisible(index, true);
            sprite.LayerSetState(index, $"reinf_construct-{stage}");
        }
    }
}
