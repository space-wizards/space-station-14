using Content.Client.Light.Components;
using Content.Shared.Light.Component;
using JetBrains.Annotations;
using Robust.Client.GameObjects;

namespace Content.Client.Light.Visualizers
{
    [UsedImplicitly]
    public class ExpendableLightVisualizer : AppearanceVisualizer
    {
        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            if (component.Deleted)
            {
                return;
            }

            if (component.TryGetData(ExpendableLightVisuals.State, out string lightBehaviourID))
            {
                if (component.Owner.TryGetComponent<LightBehaviourComponent>(out var lightBehaviour))
                {
                    lightBehaviour.StopLightBehaviour();

                    if (lightBehaviourID != string.Empty)
                    {
                        lightBehaviour.StartLightBehaviour(lightBehaviourID);
                    }
                    else if (component.Owner.TryGetComponent<PointLightComponent>(out var light))
                    {
                        light.Enabled = false;
                    }
                }
            }
        }
    }
}
