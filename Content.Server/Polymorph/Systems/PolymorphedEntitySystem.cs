using Content.Server.Actions;
using Content.Server.Body.Components;
using Content.Server.Mind;
using Content.Server.Mind.Commands;
using Content.Server.Mind.Components;
using Content.Server.Polymorph.Components;
using Content.Shared.Actions;
using Content.Shared.Actions.ActionTypes;
using Content.Shared.Damage;
using Content.Shared.MobState.Components;
using Content.Shared.MobState.State;
using Robust.Server.Containers;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Server.Polymorph.Systems
{
    public sealed class PolymorphedEntitySystem : EntitySystem
    {
        [Dependency] private readonly ActionsSystem _actions = default!;
        [Dependency] private readonly ContainerSystem _container = default!;
        [Dependency] private readonly DamageableSystem _damageable = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<PolymorphedEntityComponent, ComponentInit>(OnComponentInit);
            SubscribeLocalEvent<PolymorphedEntityComponent, AfterPolymorphEvent>(OnStartup);
            SubscribeLocalEvent<PolymorphedEntityComponent, RevertPolymorphActionEvent>(OnRevertPolymorphActionEvent);
        }

        private void OnRevertPolymorphActionEvent(EntityUid uid, PolymorphedEntityComponent component, RevertPolymorphActionEvent args)
        {
            Revert(uid);
        }

        public void Revert(EntityUid uid)
        {
            if (!TryComp<PolymorphedEntityComponent>(uid, out var component))
                return;

            for(int i = 0; i < component.ParentContainer.ContainedEntities.Count; i++)
            {
                var entity = component.ParentContainer.ContainedEntities[i];
                component.ParentContainer.Remove(entity);

                if(entity == component.Parent)
                {
                    if (TryComp<MindComponent>(uid, out var mind) && mind.Mind != null)
                    {
                        mind.Mind.TransferTo(entity);
                        
                    }
                }
            }
            QueueDel(uid);
        }

        public void GetRelativeDamage(EntityUid oldent, EntityUid newent)
        {
            if (!TryComp<DamageableComponent>(oldent, out var olddamage)
                || !TryComp<DamageableComponent>(newent, out var newdamage))
                return;

            if (!TryComp<MobStateComponent>(oldent, out var oldstate))
                return;
            
            int maxhealthold = 0;
            foreach (var state in oldstate._highestToLowestStates)
            {
                if(state.Value.IsDead())
                {
                    maxhealthold = state.Key;
                }
            }
        }

        private void OnStartup(EntityUid uid, PolymorphedEntityComponent component, AfterPolymorphEvent args)
        {
            if (component.Prototype.Forced)
                return;

            var act = new InstantAction()
            {
                Event = new RevertPolymorphActionEvent(),
                EntityIcon = component.Parent,
                Name = Loc.GetString("polymorph-revert-action-name"),
                Description = Loc.GetString("polymorph-revert-action-description"),
           };
    
            _actions.AddAction(uid, act, null);
        }

        private void OnComponentInit(EntityUid uid, PolymorphedEntityComponent component, ComponentInit args)
        {
            component.ParentContainer = _container.EnsureContainer<Container>(uid, component.Name);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            foreach (var entity in EntityQuery<PolymorphedEntityComponent>())
            {
                entity.Time += frameTime;

                if(entity.Prototype.Duration != null && entity.Time >= entity.Prototype.Duration)
                {
                    Revert(entity.Owner);
                }

                if(entity.Prototype.RevertOnDeath &&
                    TryComp<MobStateComponent>(entity.Owner, out var mob) &&
                    mob.IsDead())
                {
                    Revert(entity.Owner);
                }
            }
        }
    }

    public sealed class RevertPolymorphActionEvent : InstantActionEvent { };
}
