using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Movement.Components;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Robust.Shared.Collections;
using Robust.Shared.GameStates;

namespace Content.Server.Movement.Systems;

public sealed class JetpackSystem : SharedJetpackSystem
{
    private const float UpdateCooldown = 0.5f;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<JetpackComponent, ComponentGetState>(OnJetpackGetState);
        SubscribeLocalEvent<JetpackComponent, ComponentStartup>(OnJetpackStartup);
    }

    protected override bool CanEnable(JetpackComponent component)
    {
        return TryComp<GasTankComponent>(component.Owner, out var gasTank) && !(gasTank.Air.Pressure < component.VolumeUsage);
    }

    private void OnJetpackStartup(EntityUid uid, JetpackComponent component, ComponentStartup args)
    {
        if (component.Enabled)
        {
            component.Enabled = false;
            SetEnabled(component, component.Enabled);
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var toDisable = new ValueList<JetpackComponent>();

        foreach (var (active, comp, gasTank) in EntityQuery<ActiveJetpackComponent, JetpackComponent, GasTankComponent>())
        {
            active.Accumulator += frameTime;
            if (active.Accumulator < UpdateCooldown) continue;

            active.Accumulator -= UpdateCooldown;

            if (gasTank.Air.Pressure < comp.VolumeUsage)
            {
                toDisable.Add(comp);
                continue;
            }

            gasTank.RemoveAirVolume(comp.VolumeUsage);
        }

        foreach (var comp in toDisable)
        {
            SetEnabled(comp, false);
        }
    }

    private void OnJetpackGetState(EntityUid uid, JetpackComponent component, ref ComponentGetState args)
    {
        args.State = new JetpackComponentState()
        {
            Enabled = component.Enabled,
        };
    }
}
