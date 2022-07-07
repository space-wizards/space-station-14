using Content.Shared.Alert;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.MobState.Components;

namespace Content.Server.MobState;

public sealed partial class MobStateSystem
{
    public override void UpdateNormState(EntityUid entity, FixedPoint2 threshold)
    {
        base.UpdateNormState(entity, threshold);

        if (!TryComp<DamageableComponent>(entity, out var damageable))
            return;

        if (!TryComp<MobStateComponent>(entity, out var stateComponent))
            return;

        short modifier = 0;

        if (TryGetEarliestIncapacitatedState(stateComponent, threshold, out _, out var earliestThreshold) && damageable.TotalDamage != 0)
        {
            modifier = (short)(damageable.TotalDamage / (earliestThreshold / 5) + 1);
        }

        Alerts.ShowAlert(entity, AlertType.HumanHealth, modifier);
    }
}
