using Content.Shared.Actions;
using Content.Shared.Actions.ActionTypes;
using Content.Shared.Targeting;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.CombatMode
{
    [NetworkedComponent()]
    public abstract class SharedCombatModeComponent : Component
    {
        private bool _isInCombatMode;
        private TargetingZone _activeZone;

        [DataField("disarmFailChance")]
        public readonly float BaseDisarmFailChance = 0.4f;

        [DataField("pushChance")]
        public readonly float BasePushFailChance = 0.4f;

        [DataField("disarmFailSound")]
        public readonly SoundSpecifier DisarmFailSound = new SoundPathSpecifier("/Audio/Weapons/punchmiss.ogg");

        [DataField("disarmSuccessSound")]
        public readonly SoundSpecifier DisarmSuccessSound = new SoundPathSpecifier("/Audio/Effects/thudswoosh.ogg");

        [DataField("disarmActionId", customTypeSerializer:typeof(PrototypeIdSerializer<EntityTargetActionPrototype>))]
        public readonly string DisarmActionId = "Disarm";

        [DataField("canDisarm")]
        public bool CanDisarm;

        [DataField("disarmAction")] // must be a data-field to properly save cooldown when saving game state.
        public EntityTargetAction? DisarmAction;

        [DataField("combatToggleActionId", customTypeSerializer: typeof(PrototypeIdSerializer<InstantActionPrototype>))]
        public readonly string CombatToggleActionId = "CombatModeToggle";

        [DataField("combatToggleAction")]
        public InstantAction? CombatToggleAction;

        [ViewVariables(VVAccess.ReadWrite)]
        public virtual bool IsInCombatMode
        {
            get => _isInCombatMode;
            set
            {
                if (_isInCombatMode == value) return;
                _isInCombatMode = value;
                if (CombatToggleAction != null)
                    EntitySystem.Get<SharedActionsSystem>().SetToggled(CombatToggleAction, _isInCombatMode);
                Dirty();

                // Regenerate physics contacts -> Can probably just selectively check
                /* Still a bit jank so left disabled for now.
                if (Owner.TryGetComponent(out PhysicsComponent? physicsComponent))
                {
                    if (value)
                    {
                        physicsComponent.WakeBody();
                    }

                    physicsComponent.RegenerateContacts();
                }
                */
            }
        }

        [ViewVariables(VVAccess.ReadWrite)]
        public virtual TargetingZone ActiveZone
        {
            get => _activeZone;
            set
            {
                if (_activeZone == value) return;
                _activeZone = value;
                Dirty();
            }
        }

        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
        {
            base.HandleComponentState(curState, nextState);

            if (curState is not CombatModeComponentState state)
                return;

            IsInCombatMode = state.IsInCombatMode;
            ActiveZone = state.TargetingZone;
        }


        public override ComponentState GetComponentState()
        {
            return new CombatModeComponentState(IsInCombatMode, ActiveZone);
        }

        [Serializable, NetSerializable]
        protected sealed class CombatModeComponentState : ComponentState
        {
            public bool IsInCombatMode { get; }
            public TargetingZone TargetingZone { get; }

            public CombatModeComponentState(bool isInCombatMode, TargetingZone targetingZone)
            {
                IsInCombatMode = isInCombatMode;
                TargetingZone = targetingZone;
            }
        }
    }
}
