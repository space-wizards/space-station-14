using Content.Shared.GameObjects.Components.Mobs;
using Content.Shared.Preferences.Appearance;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;

namespace Content.Client.GameObjects.Components.Mobs
{
    [RegisterComponent]
    public sealed class HairComponent : SharedHairComponent
    {
        protected override void Startup()
        {
            base.Startup();

            UpdateHairStyle();
        }

        public override string FacialHairStyleName
        {
            get => base.FacialHairStyleName;
            set
            {
                base.FacialHairStyleName = value;
                UpdateHairStyle();
            }
        }

        public override string HairStyleName
        {
            get => base.HairStyleName;
            set
            {
                base.HairStyleName = value;
                UpdateHairStyle();
            }
        }

        public override Color HairColor
        {
            get => base.HairColor;
            set
            {
                base.HairColor = value;
                UpdateHairStyle();
            }
        }

        public override Color FacialHairColor
        {
            get => base.FacialHairColor;
            set
            {
                base.FacialHairColor = value;
                UpdateHairStyle();
            }
        }

        private void UpdateHairStyle()
        {
            var sprite = Owner.GetComponent<SpriteComponent>();

            sprite.LayerSetColor(HumanoidVisualLayers.Hair, HairColor);
            sprite.LayerSetColor(HumanoidVisualLayers.FacialHair, FacialHairColor);

            sprite.LayerSetState(HumanoidVisualLayers.Hair,
                HairStyles.HairStylesMap[HairStyleName ?? HairStyles.DefaultHairStyle]);
            sprite.LayerSetState(HumanoidVisualLayers.FacialHair,
                HairStyles.FacialHairStylesMap[FacialHairStyleName ?? HairStyles.DefaultFacialHairStyle]);
        }
    }
}
