using Content.Shared.Preferences.Appearance;
using Robust.Client.GameObjects;
using Robust.Shared.Maths;

namespace Content.Client.GameObjects.Components.Mobs
{
    public class HumanoidVisualizer2D : AppearanceVisualizer
    {
        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            var sprite = component.Owner.GetComponent<SpriteComponent>();
            {if (component.TryGetData(CharacterVisuals.HairStyle, out string styleName))
            {
                sprite.LayerSetState(HumanoidVisualLayers.Hair, HairStyles.HairStylesMap[styleName]);
            }}

            {if (component.TryGetData(CharacterVisuals.HairColor, out Color color))
            {
                sprite.LayerSetColor(HumanoidVisualLayers.Hair, color.WithAlpha(255)); // No transparent hair
            }}

            {if (component.TryGetData(CharacterVisuals.FacialHairStyle, out string styleName))
            {
                sprite.LayerSetState(HumanoidVisualLayers.FacialHair, HairStyles.FacialHairStylesMap[styleName]);
            }}

            {if (component.TryGetData(CharacterVisuals.FacialHairColor, out Color color))
            {
                sprite.LayerSetColor(HumanoidVisualLayers.FacialHair, color.WithAlpha(255)); // No transparent hair
            }}
        }
    }
}
