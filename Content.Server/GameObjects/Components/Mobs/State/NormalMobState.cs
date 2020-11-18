#nullable enable
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.Alert;
using Content.Shared.GameObjects.Components.Damage;
using Content.Shared.GameObjects.Components.Mobs;
using Content.Shared.GameObjects.Components.Mobs.State;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Server.GameObjects.Components.Mobs.State
{
    public class NormalMobState : SharedNormalMobState
    {
        public override void EnterState(IEntity entity)
        {
            base.EnterState(entity);

            EntitySystem.Get<StandingStateSystem>().Standing(entity);

            if (entity.TryGetComponent(out AppearanceComponent? appearance))
            {
                appearance.SetData(DamageStateVisuals.State, DamageState.Alive);
            }
        }

        public override void UpdateState(IEntity entity, int threshold)
        {
            base.UpdateState(entity, threshold);

            if (!entity.TryGetComponent(out IDamageableComponent? damageable))
            {
                return;
            }

            if (!entity.TryGetComponent(out ServerAlertsComponent? alerts))
            {
                return;
            }

            if (!entity.TryGetComponent(out IMobStateComponent? stateComponent))
            {
                return;
            }

            short modifier = 0;

            if (stateComponent.TryGetEarliestIncapacitatedState(threshold, out _, out var earliestThreshold))
            {
                modifier = (short) (damageable.TotalDamage / (earliestThreshold / 7f));
            }

            alerts.ShowAlert(AlertType.HumanHealth, modifier);
        }
    }
}
