using Content.Shared.GameObjects.Components.Atmos;
using Robust.Client.GameObjects;
using Robust.Client.Interfaces.GameObjects.Components;

namespace Content.Client.GameObjects.Components.Atmos
{
    class GasAnalyzerVisualizer : AppearanceVisualizer
    {
        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            if (component.Deleted)
            {
                return;
            }

            if (!component.Owner.TryGetComponent(out ISpriteComponent sprite))
            {
                return;
            }

            if (component.TryGetData(GasAnalyzerVisuals.VisualState, out GasAnalyzerVisualState visualState))
            {
                switch (visualState)
                {
                    case GasAnalyzerVisualState.Off:
                        sprite.LayerSetState(0, "icon");
                        break;
                    case GasAnalyzerVisualState.Working:
                        sprite.LayerSetState(0, "working");
                        break;
                    default:
                        break;
                }
            }
        }
    }
}
