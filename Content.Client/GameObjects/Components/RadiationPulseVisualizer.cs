using Content.Client.GameObjects.Components.Radiation;
using Content.Shared.GameObjects.Components;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Graphics.Shaders;
using Robust.Client.Interfaces.GameObjects.Components;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace Content.Client.GameObjects.Components
{
    [UsedImplicitly]
    public class RadiationPulseVisualizer : AppearanceVisualizer
    {
        private ShaderInstance _shader;
        public override void LoadData(YamlMappingNode node)
        {
            base.LoadData(node);
            if (node.TryGetNode("shader", out var shaderId))
            {
                var shader = shaderId.AsString();
                if (!string.IsNullOrEmpty(shader))
                {
                    _shader = IoCManager.Resolve<IPrototypeManager>().Index<ShaderPrototype>(shader).Instance();
                }
            }
        }

        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);
            if (component.Deleted)
            {
                return;
            }

            if (!component.TryGetData(RadiationPulseVisual.State, out RadiationPulseVisuals state))
            {
                state = RadiationPulseVisuals.None;
            }

            switch (state)
            {
                case RadiationPulseVisuals.None:
                    break;
                case RadiationPulseVisuals.Visible:
                    var sprite = component.Owner.GetComponent<ISpriteComponent>();
                    if (_shader != null)
                    {
                        sprite.PostShader = _shader;
                    }

                    var pointLight = component.Owner.GetComponent<PointLightComponent>();
                    var radiationPulse = component.Owner.GetComponent<RadiationPulseComponent>();
                    pointLight.Radius = radiationPulse.Range;

                    var lightBehaviour = component.Owner.GetComponent<LightBehaviourComponent>();
                    lightBehaviour?.StopLightBehaviour(removeBehaviour:true);
                    lightBehaviour?.AddNewLightBehaviour(
                            new FadesInOutNLevelBehaviour()
                            {
                                MainColor = Color.Green,
                                Levels = 20,
                                MaxDuration = (float)((radiationPulse.EndTime - radiationPulse.StartTime).TotalSeconds)
                            }, pointLight
                        );
                    break;
                default:
                    break;
            }
        }
    }
}
