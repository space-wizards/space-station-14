using Content.Server.ParticleAccelerator.Components;
using Content.Server.ParticleAccelerator.EntitySystems;
using Content.Server.Popups;
using Content.Server.Wires;
using Content.Shared.Popups;
using Content.Shared.Singularity.Components;
using Content.Shared.Wires;

namespace Content.Server.ParticleAccelerator.Wires;

public sealed partial class ParticleAcceleratorLimiterWireAction : ComponentWireAction<ParticleAcceleratorControlBoxComponent>
{
    public override string Name { get; set; } = "wire-name-pa-limiter";
    public override Color Color { get; set; } = Color.Teal;
    public override object StatusKey { get; } = ParticleAcceleratorWireStatus.Limiter;

    public override StatusLightData? GetStatusLightData(Wire wire)
    {
        var result = base.GetStatusLightData(wire);

        if (result.HasValue
        && EntityManager.TryGetComponent<ParticleAcceleratorControlBoxComponent>(wire.Owner, out var controller)
        && controller.MaxStrength >= ParticleAcceleratorPowerState.Level3)
            result = new(Color.Purple, result.Value.State, result.Value.Text);

        return result;
    }

    public override StatusLightState? GetLightState(Wire wire, ParticleAcceleratorControlBoxComponent component)
    {
        return StatusLightState.On;
    }

    public override bool Cut(EntityUid user, Wire wire, ParticleAcceleratorControlBoxComponent controller)
    {
        controller.MaxStrength = ParticleAcceleratorPowerState.Level3;
        var paSystem = EntityManager.System<ParticleAcceleratorSystem>();
        paSystem.UpdateUI(wire.Owner, controller);
        return true;
    }

    public override bool Mend(EntityUid user, Wire wire, ParticleAcceleratorControlBoxComponent controller)
    {

        controller.MaxStrength = ParticleAcceleratorPowerState.Level2;
        if (controller.SelectedStrength <= controller.MaxStrength || controller.StrengthLocked)
            return true;

        // Yes, it's a feature that mending this wire WON'T WORK if the strength wire is also cut.
        // Since that blocks SetStrength().
        var paSystem = EntityManager.System<ParticleAcceleratorSystem>();
        paSystem.SetStrength(wire.Owner, controller.MaxStrength, user, controller);
        paSystem.UpdateUI(wire.Owner, controller);
        return true;
    }

    public override void Pulse(EntityUid user, Wire wire, ParticleAcceleratorControlBoxComponent controller)
    {
        EntityManager.System<PopupSystem>()
            .PopupEntity(
            Loc.GetString("particle-accelerator-control-box-component-wires-update-limiter-on-pulse"),
            user,
            PopupType.SmallCaution
        );
    }

    public override void Update(Wire wire)
    {

    }
}
