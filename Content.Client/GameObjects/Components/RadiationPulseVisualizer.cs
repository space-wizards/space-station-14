using Content.Client.GameObjects.Components.Radiation;
using Content.Shared.GameObjects.Components;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.Maths;

namespace Content.Client.GameObjects.Components
{
    [UsedImplicitly]
    public class RadiationPulseVisualizer : AppearanceVisualizer
    {
        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            if (!component.TryGetData(RadiationPulseVisual.State, out RadiationPulseVisuals state))
            {
                state = RadiationPulseVisuals.None;
            }

            switch (state)
            {
                case RadiationPulseVisuals.None:
                    break;
                case RadiationPulseVisuals.Visible:
                    var entity = component.Owner;
                    if (!entity.TryGetComponent(out PointLightComponent pointLight))
                    {
                        return;
                    }
                    if (!entity.TryGetComponent(out RadiationPulseComponent radiationPulse))
                    {
                        return;
                    }
                    pointLight.Radius = radiationPulse.Range;

                    if (entity.TryGetComponent (out LightBehaviourComponent lightBehaviour))
                    {
                        lightBehaviour.StopLightBehaviour(removeBehaviour:true);
                        lightBehaviour.AddNewLightBehaviour(
                            new FadesInOutNLevelBehaviour()
                            {
                                MainColor = Color.Green,
                                Levels = 20,
                                MaxDuration = (float)((radiationPulse.EndTime - radiationPulse.StartTime).TotalSeconds)
                            }, pointLight
                        );
                    }
                    break;
                default:
                    break;
            }
        }
    }
}
