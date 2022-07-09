using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Body.Components;
using Content.Shared.Atmos;

namespace Content.Server.Body.Systems;

public sealed class InternalsSystem : EntitySystem
{
    [Dependency] private readonly AtmosphereSystem _atmos = default!;
    [Dependency] private readonly GasTankSystem _gasTank = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<InternalsComponent, InhaleLocationEvent>(OnInhaleLocation);
    }

    private void OnInhaleLocation(EntityUid uid, InternalsComponent component, InhaleLocationEvent args)
    {
        if (AreInternalsWorking(component))
        {
            var gasTank = Comp<GasTankComponent>(component.GasTankEntity!.Value);
            args.Gas = _gasTank.RemoveAirVolume(gasTank, Atmospherics.BreathVolume);
        }
    }
    public void DisconnectBreathTool(InternalsComponent component)
    {
        var old = component.BreathToolEntity;
        component.BreathToolEntity = null;

        if (TryComp(old, out BreathToolComponent? breathTool) )
        {
            _atmos.DisconnectInternals(breathTool);
            DisconnectTank(component);
        }
    }

    public void ConnectBreathTool(InternalsComponent component, EntityUid toolEntity)
    {
        if (TryComp(component.BreathToolEntity, out BreathToolComponent? tool))
        {
            _atmos.DisconnectInternals(tool);
        }

        component.BreathToolEntity = toolEntity;
    }

    public void DisconnectTank(InternalsComponent? component)
    {
        if (component == null) return;

        if (TryComp(component.GasTankEntity, out GasTankComponent? tank))
        {
            _gasTank.DisconnectFromInternals(tank, component.Owner);
        }

        component.GasTankEntity = null;
    }

    public bool TryConnectTank(InternalsComponent component, EntityUid tankEntity)
    {
        if (component.BreathToolEntity == null)
            return false;

        if (TryComp(component.GasTankEntity, out GasTankComponent? tank))
        {
            _gasTank.DisconnectFromInternals(tank, component.Owner);
        }

        component.GasTankEntity = tankEntity;
        return true;
    }

    public bool AreInternalsWorking(InternalsComponent component)
    {
        return TryComp(component.BreathToolEntity, out BreathToolComponent? breathTool) &&
               breathTool.IsFunctional &&
               TryComp(component.GasTankEntity, out GasTankComponent? gasTank) &&
               gasTank.Air != null;
    }
}
