using Content.Shared.Actions;
using Content.Shared.Actions.ActionTypes;
using Content.Shared.Targeting;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Utility;

namespace Content.Shared.CombatMode
{
    [NetworkedComponent()]
    public abstract class SharedCombatModeComponent : Component
    {
        [ViewVariables, DataField("precisionAction")]
        public InstantAction? PrecisionAction = new()
        {
            Icon = new SpriteSpecifier.Rsi(new ResourcePath("Objects/Weapons/Melee/spear.rsi"), "spear"),
            Name = "action-name-precision",
            Description = "action-description-precision",
            UserPopup = "action-popup-precision",
            PopupToggleSuffix = "-enabled",
            // TODO: Needs a sound for toggle
            Event = new TogglePrecisionModeEvent(),
        };

        /// <summary>
        /// Precision targeting. When precision is enabled we attack what our mouse is over rather than as an arc attack.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite), DataField("precisionMode")]
        public bool PrecisionMode;

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
    }
}
