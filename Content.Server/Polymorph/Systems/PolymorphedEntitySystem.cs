using Content.Server.Actions;
using Content.Server.Mind.Components;
using Content.Server.Polymorph.Components;
using Content.Server.Popups;
using Content.Shared.Actions;
using Content.Shared.Actions.ActionTypes;
using Content.Shared.Damage;
using Content.Shared.MobState.Components;
using Robust.Shared.Player;

namespace Content.Server.Polymorph.Systems
{
    public sealed class PolymorphedEntitySystem : EntitySystem
    {
        [Dependency] private readonly ActionsSystem _actions = default!;
        [Dependency] private readonly DamageableSystem _damageable = default!;
        [Dependency] private readonly PopupSystem _popup = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<PolymorphedEntityComponent, PolymorphComponentSetupEvent>(OnInit);
            SubscribeLocalEvent<PolymorphedEntityComponent, RevertPolymorphActionEvent>(OnRevertPolymorphActionEvent);
        }

        private void OnRevertPolymorphActionEvent(EntityUid uid, PolymorphedEntityComponent component, RevertPolymorphActionEvent args)
        {
            Revert(uid);
        }

        /// <summary>
        /// Reverts a polymorphed entity back into its original form
        /// </summary>
        /// <param name="uid">The entityuid of the entity being reverted</param>
        public void Revert(EntityUid uid)
        {
            if (!TryComp<PolymorphedEntityComponent>(uid, out var component))
                return;

            var uidXform = Transform(uid);
            var parentXform = Transform(component.Parent);

            parentXform.AttachParent(uidXform.ParentUid);
            parentXform.Coordinates = uidXform.Coordinates;

            if (TryComp<DamageableComponent>(component.Parent, out var damageParent) &&
                _damageable.GetScaledDamage(uid, component.Parent, out var damage) &&
                damage != null)
            {
                _damageable.SetDamage(damageParent, damage);
            }

            if (TryComp<MindComponent>(uid, out var mind) && mind.Mind != null)
            {
                mind.Mind.TransferTo(component.Parent);
            }

            _popup.PopupEntity(Loc.GetString("polymorph-revert-popup-generic", ("parent", uid), ("child", component.Parent)), component.Parent, Filter.Pvs(component.Parent));
            QueueDel(uid);
        }

        private void OnInit(EntityUid uid, PolymorphedEntityComponent component, PolymorphComponentSetupEvent args)
        {
            if (component.Prototype.Forced)
                return;

            var act = new InstantAction()
            {
                Event = new RevertPolymorphActionEvent(),
                EntityIcon = component.Parent,
                Name = Loc.GetString("polymorph-revert-action-name"),
                Description = Loc.GetString("polymorph-revert-action-description"),
                UseDelay = TimeSpan.FromSeconds(component.Prototype.Delay),
           };
    
            _actions.AddAction(uid, act, null);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            foreach (var entity in EntityQuery<PolymorphedEntityComponent>())
            {
                entity.Time += frameTime;

                if(entity.Prototype.Duration != null && entity.Time >= entity.Prototype.Duration)
                    Revert(entity.Owner);

                if (!TryComp<MobStateComponent>(entity.Owner, out var mob))
                    continue;

                if ((entity.Prototype.RevertOnDeath && mob.IsDead()) ||
                    (entity.Prototype.RevertOnCrit && mob.IsCritical()))
                    Revert(entity.Owner);
            }
        }
    }

    public sealed class RevertPolymorphActionEvent : InstantActionEvent { };
}
