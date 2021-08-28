using Content.Server.Light.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.Player;

namespace Content.Server.Light.EntitySystems
{
    public class UnpoweredFlashlightSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();
        }

        public void ToggleLight(UnpoweredFlashlightComponent flashlight)
        {
            if (!flashlight.Owner.TryGetComponent(out PointLightComponent? light))
                return;

            flashlight.LightOn = !flashlight.LightOn;
            light.Enabled = flashlight.LightOn;
            SoundSystem.Play(Filter.Pvs(light.Owner), flashlight.ToggleSound.GetSound(), flashlight.Owner);
        }

    }
}
