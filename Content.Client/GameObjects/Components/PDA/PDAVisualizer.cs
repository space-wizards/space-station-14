using Content.Shared.GameObjects.Components.PDA;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Interfaces.GameObjects.Components;

namespace Content.Client.GameObjects.Components.PDA
{
    [UsedImplicitly]
    // ReSharper disable once InconsistentNaming
    public class PDAVisualizer : AppearanceVisualizer
    {

        private enum PDAVisualLayers
        {
            Base,
            Flashlight,
            IDLight
        }


        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);
            if (component.Owner.Deleted)
            {
                return;
            }
            var sprite = component.Owner.GetComponent<ISpriteComponent>();
            sprite.LayerSetVisible(PDAVisualLayers.Flashlight, false);
            if (component.TryGetData(PDAVisuals.FlashlightLit, out bool isScreenLit))
            {
                sprite.LayerSetState(PDAVisualLayers.Flashlight, "light_overlay");
                sprite.LayerSetVisible(PDAVisualLayers.Flashlight, isScreenLit);
            }

            if (component.TryGetData(PDAVisuals.IDCardInserted, out bool isCardInserted))
            {
                sprite.LayerSetVisible(PDAVisualLayers.IDLight, isCardInserted);
            }

        }


    }
}
