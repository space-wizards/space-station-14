using Content.Shared.GameObjects.Components.Mobs;
using Content.Shared.Preferences;
using Content.Shared.Preferences.Appearance;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;

namespace Content.Client.GameObjects.Components.Mobs
{
    [RegisterComponent]
    public sealed class HumanoidAppearanceComponent : SharedHumanoidAppearanceComponent
    {
        public override HumanoidCharacterAppearance Appearance
        {
            get => base.Appearance;
            set
            {
                base.Appearance = value;
                UpdateLooks();
            }
        }

        public override Sex Sex
        {
            get => base.Sex;
            set
            {
                base.Sex = value;
                UpdateLooks();
            }
        }

        protected override void Startup()
        {
            base.Startup();

            UpdateLooks();
        }

        private void UpdateLooks()
        {
            if (Appearance is null) return;
            var sprite = Owner.GetComponent<SpriteComponent>();

            sprite.LayerSetColor(HumanoidVisualLayers.Hair, Appearance.HairColor);
            sprite.LayerSetColor(HumanoidVisualLayers.FacialHair, Appearance.FacialHairColor);

            sprite.LayerSetState(HumanoidVisualLayers.Chest, Sex == Sex.Male ? "human_chest_m" : "human_chest_f");
            sprite.LayerSetState(HumanoidVisualLayers.Head, Sex == Sex.Male ? "human_head_m" : "human_head_f");

            sprite.LayerSetVisible(HumanoidVisualLayers.StencilMask, Sex == Sex.Female);

            var hairStyle = Appearance.HairStyleName;
            if (string.IsNullOrWhiteSpace(hairStyle) || !HairStyles.HairStylesMap.ContainsKey(hairStyle))
                hairStyle = HairStyles.DefaultHairStyle;
            sprite.LayerSetState(HumanoidVisualLayers.Hair,
                HairStyles.HairStylesMap[hairStyle]);

            var facialHairStyle = Appearance.FacialHairStyleName;
            if (string.IsNullOrWhiteSpace(facialHairStyle) || !HairStyles.FacialHairStylesMap.ContainsKey(facialHairStyle))
                facialHairStyle = HairStyles.DefaultFacialHairStyle;
            sprite.LayerSetState(HumanoidVisualLayers.FacialHair,
                HairStyles.FacialHairStylesMap[facialHairStyle]);
        }
    }
}
