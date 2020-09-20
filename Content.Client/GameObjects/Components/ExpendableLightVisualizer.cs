
using Content.Shared.GameObjects.Components;
using JetBrains.Annotations;
using Robust.Client.GameObjects;

namespace Content.Client.GameObjects.Components
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
