using Content.Shared.Interaction.Events;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs;
using Robust.Shared.GameStates;
using Content.Shared.Mobs.Systems;
using Content.Shared.Actions.ActionTypes;

namespace Content.Shared.SCP.ConcreteSlab
{
    public abstract class SharedSCP173System : EntitySystem
    {
        [Dependency] private readonly MobStateSystem _mobState = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<SharedSCP173Component, ComponentGetState>(OnGetState);
            SubscribeLocalEvent<SharedSCP173Component, ComponentHandleState>(OnHandleState);
        }

        private void OnGetState(EntityUid uid, SharedSCP173Component component, ref ComponentGetState args)
        {
            args.State = new SCP173ComponentState(component.Enabled, component.LookedAt);
        }
        private void OnHandleState(EntityUid uid, SharedSCP173Component component, ref ComponentHandleState args)
        {
            if (args.Current is not SCP173ComponentState state) return;
            if (component.LookedAt != state.LookedAt)
            {
                var ev = new OnLookStateChangedEvent(state.LookedAt);
                RaiseLocalEvent(ev);
            };
            component.Enabled = state.Enabled;
            component.LookedAt = state.LookedAt;
        }

        protected bool CanAttack(EntityUid uid,EntityUid trg ,SharedSCP173Component component)
        {
            return
                !component.LookedAt &&
                trg.IsValid() &&
                trg != uid &&
                HasComp<MobStateComponent>(trg) &&
                !_mobState.IsDead(trg);
        }
    }
    public sealed class OnLookStateChangedEvent : EntityEventArgs
    {
        public bool IsLookedAt;
        public OnLookStateChangedEvent(bool islookedat)
        {
            IsLookedAt = islookedat;
        }
    }
}
