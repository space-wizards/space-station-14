using Content.Shared.GameObjects.Components.Mobs;
using Content.Shared.Preferences.Appearance;
using Robust.Client.GameObjects;
using Robust.Client.Graphics.Shaders;
using Robust.Client.Interfaces.GameObjects.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;

namespace Content.Client.GameObjects.Components.Mobs
{
    [RegisterComponent]
    public sealed class HairComponent : SharedHairComponent
    {
        private const string HairShaderName = "hair";
        private const string HairColorParameter = "hairColor";

#pragma warning disable 649
        [Dependency] private readonly IPrototypeManager _prototypeManager;
#pragma warning restore 649

        private ShaderInstance _facialHairShader;
        private ShaderInstance _hairShader;

        public override void Initialize()
        {
            base.Initialize();

            var shaderProto = _prototypeManager.Index<ShaderPrototype>(HairShaderName);

            _facialHairShader = shaderProto.InstanceUnique();
            _hairShader = shaderProto.InstanceUnique();
        }

        protected override void Startup()
        {
            base.Startup();

            if (Owner.TryGetComponent(out ISpriteComponent sprite))
            {
                sprite.LayerSetShader(HumanoidVisualLayers.Hair, _hairShader);
                sprite.LayerSetShader(HumanoidVisualLayers.FacialHair, _facialHairShader);
            }

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

            _hairShader?.SetParameter(HairColorParameter, HairColor);
            _facialHairShader?.SetParameter(HairColorParameter, FacialHairColor);

            sprite.LayerSetState(HumanoidVisualLayers.Hair,
                HairStyles.HairStylesMap[HairStyleName ?? HairStyles.DefaultHairStyle]);
            sprite.LayerSetState(HumanoidVisualLayers.FacialHair,
                HairStyles.FacialHairStylesMap[FacialHairStyleName ?? HairStyles.DefaultFacialHairStyle]);
        }
    }
}
