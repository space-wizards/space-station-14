#nullable enable
using Content.Shared.GameObjects.Components.Power.ApcNetComponents.PowerReceiverUsers;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.Maths;

namespace Content.Client.GameObjects.Components.Power.ApcNetComponents.PowerReceiverUsers
{
    [UsedImplicitly]
    public class PoweredLightVisualizer : AppearanceVisualizer
    {
        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            if (!component.Owner.TryGetComponent(out ISpriteComponent? sprite)) return;

            if (!component.Owner.TryGetComponent(out PointLightComponent? light)) return;

            if (!component.TryGetData(PoweredLightVisuals.BulbState, out PoweredLightState state)) return;

            switch (state)
            {
                case PoweredLightState.Empty:
                    sprite.LayerSetState(0, "empty");
                    light.Enabled = false;
                    break;
                case PoweredLightState.Off:
                    sprite.LayerSetState(0, "off");
                    light.Enabled = false;
                    break;
                case PoweredLightState.On:
                    sprite.LayerSetState(0, "on");
                    light.Enabled = true;
                    if (component.TryGetData(PoweredLightVisuals.BulbColor, out Color color))
                        light.Color = color;
                    break;
                case PoweredLightState.Broken:
                    sprite.LayerSetState(0, "broken");
                    light.Enabled = false;
                    break;
                case PoweredLightState.Burned:
                    sprite.LayerSetState(0, "burn");
                    light.Enabled = false;
                    break;
            }
        }
    }
}
