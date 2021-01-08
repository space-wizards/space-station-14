using Content.Shared.GameObjects.Components;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Interfaces.GameObjects.Components;
using Robust.Shared.Interfaces.Serialization;

namespace Content.Client.GameObjects.Components
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

        public override IDeepClone DeepClone()
        {
            return new ReinforcedWallVisualizer();
        }

        public void SetDeconstructionStage(AppearanceComponent component, int stage)
        {
            var entity = component.Owner;

            if (!entity.TryGetComponent(out ISpriteComponent sprite)) return;

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
