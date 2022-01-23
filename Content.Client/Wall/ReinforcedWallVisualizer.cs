using Content.Shared.Wall;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Client.Wall
{
    [UsedImplicitly]
    public class ReinforcedWallVisualizer : AppearanceVisualizer
    {
        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            if (component.TryGetData(ReinforcedWallVisuals.DeconstructionStage, out int stage))
            {
                SetDeconstructionStage(component, stage);
            }
        }

        public void SetDeconstructionStage(AppearanceComponent component, int stage)
        {
            var entity = component.Owner;

            var entities = IoCManager.Resolve<IEntityManager>();
            if (!entities.TryGetComponent(entity, out ISpriteComponent? sprite)) return;

            if (stage < 0)
            {
                sprite.LayerSetVisible(ReinforcedWallVisualLayers.Deconstruction, false);
                return;
            }

            sprite.LayerSetVisible(ReinforcedWallVisualLayers.Deconstruction, true);
            sprite.LayerSetState(ReinforcedWallVisualLayers.Deconstruction, $"reinf_construct-{stage}");
        }
    }

    public enum ReinforcedWallVisualLayers : byte
    {
        Deconstruction,
    }
}
