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
            Unlit
        }


        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);
            if (component.Owner.Deleted)
            {
                return;
            }
            var sprite = component.Owner.GetComponent<ISpriteComponent>();
            sprite.LayerSetVisible(PDAVisualLayers.Unlit, false);
            if(!component.TryGetData<bool>(PDAVisuals.ScreenLit, out var isScreenLit))
            {
                return;
            }
            sprite.LayerSetState(PDAVisualLayers.Unlit, "unlit_pda_screen");
            sprite.LayerSetVisible(PDAVisualLayers.Unlit, isScreenLit);


        }


    }
}
