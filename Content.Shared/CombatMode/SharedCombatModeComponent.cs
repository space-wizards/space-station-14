using System;
using Content.Shared.Actions;
using Content.Shared.Actions.ActionTypes;
using Content.Shared.Sound;
using Content.Shared.Targeting;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.CombatMode
{
    [NetworkedComponent()]
    public abstract class SharedCombatModeComponent : Component
    {
        private bool _isInCombatMode;
        private TargetingZone _activeZone;

        [DataField("disarmFailChance")]
        public readonly float DisarmFailChance = 0.4f;

        [DataField("pushChance")]
        public readonly float DisarmPushChance = 0.4f;

        [DataField("disarmFailSound")]
        public readonly SoundSpecifier DisarmFailSound = new SoundPathSpecifier("/Audio/Weapons/punchmiss.ogg");

        [DataField("disarmSuccessSound")]
        public readonly SoundSpecifier DisarmSuccessSound = new SoundPathSpecifier("/Audio/Effects/thudswoosh.ogg");

        // These are chonky default definitions for combat actions. But its a pain to add a yaml version of this for
        // every entity that wants combat mode, especially given that they're currently all identical... so ummm.. yeah.
        [DataField("disarmAction")]
        public readonly EntityTargetAction DisarmAction = new()
        {
            Icon = new SpriteSpecifier.Texture(new ResourcePath("Interface/Actions/disarmOff.png")),
            IconOn = new SpriteSpecifier.Texture(new ResourcePath("Interface/Actions/disarm.png")),
            Name = "action-name-disarm",
            Description = "action-description-disarm",
            Repeat = true,
            UseDelay = TimeSpan.FromSeconds(1.5f),
            InteractOnMiss = true,
            Event = new DisarmActionEvent(),
            CanTargetSelf = false,
            Whitelist = new()
            {
                Components = new[] { "Hands", "StatusEffects" },
            },
        };

        [DataField("combatToggleAction")]
        public readonly InstantAction CombatToggleAction = new()
        {
            Icon = new SpriteSpecifier.Texture(new ResourcePath("Interface/Actions/harmOff.png")),
            IconOn = new SpriteSpecifier.Texture(new ResourcePath("Interface/Actions/harm.png")),
            UserPopup = "action-popup-combat",
            PopupToggleSuffix = "-disabling",
            Name = "action-name-combat",
            Description = "action-description-combat",
            Event = new ToggleCombatActionEvent(),
        };

        [ViewVariables(VVAccess.ReadWrite)]
        public virtual bool IsInCombatMode
        {
            get => _isInCombatMode;
            set
            {
                if (_isInCombatMode == value) return;
                _isInCombatMode = value;
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
