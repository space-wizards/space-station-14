using Content.Shared.GameObjects.Components.Nutrition;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Interfaces.GameObjects.Components;

namespace Content.Client.GameObjects.Components.Nutrition
{
    [UsedImplicitly]
    public class CreamPiedVisualizer : AppearanceVisualizer
    {
        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            if (component.TryGetData<bool>(CreamPiedVisuals.Creamed, out var pied))
            {
                SetPied(component, pied);
            }
        }

        private void SetPied(AppearanceComponent component, bool pied)
        {
            var sprite = component.Owner.GetComponent<ISpriteComponent>();

            sprite.LayerSetVisible(CreamPiedVisualLayers.Pie, pied);
        }
    }

    public enum CreamPiedVisualLayers
    {
        Pie,
    }
}
