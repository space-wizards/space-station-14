using Content.Shared.Actions;
using Content.Shared.Actions.ActionTypes;
using Content.Shared.Targeting;
using Content.Shared.Verbs;
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
        #region Disarm

        /// <summary>
        /// Whether we are able to disarm. This requires our active hand to be free.
        /// False if it's toggled off for whatever reason, null if it's not possible.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite), DataField("canDisarm")]
        public bool? CanDisarm;

        [DataField("disarmSuccessSound")]
        public readonly SoundSpecifier DisarmSuccessSound = new SoundPathSpecifier("/Audio/Effects/thudswoosh.ogg");

        [DataField("disarmFailChance")]
        public readonly float BaseDisarmFailChance = 0.75f;

        #endregion

        private bool _isInCombatMode;
        private TargetingZone _activeZone;

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
