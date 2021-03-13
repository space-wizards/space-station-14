#nullable enable
using Content.Shared.GameObjects.Components.Chemistry;
using JetBrains.Annotations;
using Robust.Client.GameObjects;


namespace Content.Client.GameObjects.Components.Chemistry
{
    [UsedImplicitly]
    public class SolutionContainerCupVisualizer : AppearanceVisualizer
    {
        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            if (!component.TryGetData(SolutionContainerVisuals.IsCapClosed,out bool isCapClosed)) return;

            if (!component.Owner.TryGetComponent(out ISpriteComponent? sprite)) return;

            if (!sprite.LayerMapTryGet(SolutionContainerLayers.Cap, out var fillLayer)) return;

            sprite.LayerSetVisible(fillLayer, isCapClosed);
        }
    }
}
