using Content.Shared.Atmos.Piping;
using JetBrains.Annotations;
using Robust.Client.GameObjects;

namespace Content.Client.Atmos.Visualizers
{
    [UsedImplicitly]
    public sealed class PipeColorVisualizer : AppearanceVisualizer
    {
        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            if (!IoCManager.Resolve<IEntityManager>().TryGetComponent(component.Owner, out SpriteComponent? sprite))
                return;

            if (component.TryGetData(PipeColorVisuals.Color, out Color color))
            {
                // T-ray scanner / sub floor runs after this visualizer. Lets not bulldoze transparency.
                var layer = sprite[Layers.Pipe];
                layer.Color = color.WithAlpha(layer.Color.A);
            }
        }

        public enum Layers : byte
        {
            Pipe,
        }
    }
}
