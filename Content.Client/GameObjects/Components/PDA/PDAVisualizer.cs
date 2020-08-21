using Content.Shared.GameObjects.Components.PDA;
using Robust.Client.GameObjects;
using Robust.Client.Interfaces.GameObjects.Components;

namespace Content.Client.GameObjects.Components.PDA
{
    public class PDAVisualizer : AppearanceVisualizer
    {

        private enum PDAVisualLayers
        {
            Base,
            Flashlight
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
            if(!component.TryGetData<bool>(PDAVisuals.FlashlightLit, out var isScreenLit))
            {
                return;
            }
            sprite.LayerSetState(PDAVisualLayers.Flashlight, "light_overlay");
            sprite.LayerSetVisible(PDAVisualLayers.Flashlight, isScreenLit);


        }


    }
}
