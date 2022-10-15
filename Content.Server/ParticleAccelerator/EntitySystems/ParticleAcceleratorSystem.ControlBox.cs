using Content.Server.ParticleAccelerator.Components;
using Content.Server.Power.Components;

namespace Content.Server.ParticleAccelerator.EntitySystems;

public sealed partial class ParticleAcceleratorSystem
{
    private void InitializeControlBoxSystem()
    {
        SubscribeLocalEvent<ParticleAcceleratorControlBoxComponent, PowerChangedEvent>(OnControlBoxPowerChange);
    }

    private static void OnControlBoxPowerChange(EntityUid uid, ParticleAcceleratorControlBoxComponent component, ref PowerChangedEvent args)
    {
        component.OnPowerStateChanged(args);
    }
}
