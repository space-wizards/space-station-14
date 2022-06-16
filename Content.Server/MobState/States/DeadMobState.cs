using Content.Shared.Alert;
using Content.Shared.MobState.State;
using Content.Shared.StatusEffect;
using Content.Shared.Popups;
using Robust.Shared.Player;

namespace Content.Server.MobState.States
{
    public sealed class DeadMobState : SharedDeadMobState
    {
        [Dependency] private readonly SharedPopupSystem _popup = default!;

        public override void EnterState(EntityUid uid, IEntityManager entityManager)
        {
            base.EnterState(uid, entityManager);

            EntitySystem.Get<AlertsSystem>().ShowAlert(uid, AlertType.HumanDead);
            var popup = entityManager.EntitySysManager.GetEntitySystem<SharedPopupSystem>();

            // Create the popup (Disabled for now due to popups not being obscured by LOS)

            //popup.PopupEntity(Loc.GetString("chat-manager-entity-death-message",
            //    ("entityName", uid)), uid, Filter.Pvs(uid, entityManager: entityManager));

            if (entityManager.TryGetComponent(uid, out StatusEffectsComponent? stun))
            {
                EntitySystem.Get<StatusEffectsSystem>().TryRemoveStatusEffect(uid, "Stun");
            }
        }
    }
}
