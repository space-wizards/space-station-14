using Content.Server.Atmos.Components;
using Content.Shared.Alert;
using Content.Shared.Atmos.Components;
using Content.Shared.Atmos.EntitySystems;
using Content.Shared.Body.Components;
using Robust.Shared.Utility;

namespace Content.Shared.Body.Systems;

public abstract class SharedInternalsSystem : EntitySystem
{
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly SharedGasTankSystem _gasTank = default!;

    public void DisconnectTank(Entity<InternalsComponent> ent)
    {
        if (TryComp(ent.Comp.GasTankEntity, out GasTankComponent? tank))
            _gasTank.DisconnectFromInternals((ent.Comp.GasTankEntity.Value, tank));

        ent.Comp.GasTankEntity = null;
        _alerts.ShowAlert(ent.Owner, ent.Comp.InternalsAlert, GetSeverity(ent.Comp));
    }

    public bool TryConnectTank(Entity<InternalsComponent> ent, EntityUid tankEntity)
    {
        if (ent.Comp.BreathTools.Count == 0)
            return false;

        if (TryComp(ent.Comp.GasTankEntity, out GasTankComponent? tank))
            _gasTank.DisconnectFromInternals((ent.Comp.GasTankEntity.Value, tank));

        ent.Comp.GasTankEntity = tankEntity;
        _alerts.ShowAlert(ent, ent.Comp.InternalsAlert, GetSeverity(ent));
        return true;
    }

    public bool AreInternalsWorking(EntityUid uid, InternalsComponent? component = null)
    {
        return Resolve(uid, ref component, logMissing: false)
               && AreInternalsWorking(component);
    }

    public bool AreInternalsWorking(InternalsComponent component)
    {
        return TryComp(component.BreathTools.FirstOrNull(), out BreathToolComponent? breathTool)
               && breathTool.IsFunctional
               && HasComp<GasTankComponent>(component.GasTankEntity);
    }

    protected short GetSeverity(InternalsComponent component)
    {
        if (component.BreathTools.Count == 0 || !AreInternalsWorking(component))
            return 2;

        // If pressure in the tank is below low pressure threshold, flash warning on internals UI
        if (TryComp<GasTankComponent>(component.GasTankEntity, out var gasTank)
            && gasTank.IsLowPressure)
        {
            return 0;
        }

        return 1;
    }
}
