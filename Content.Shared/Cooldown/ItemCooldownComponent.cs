using System;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Players;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Shared.Cooldown
{
    /// <summary>
    ///     Stores a visual "cooldown" for items, that gets displayed in the hands GUI.
    /// </summary>
    [RegisterComponent]
    [NetworkedComponent()]
    public sealed class ItemCooldownComponent : Component
    {
        public override string Name => "ItemCooldown";

        private TimeSpan? _cooldownEnd;
        private TimeSpan? _cooldownStart;

        /// <summary>
        ///     The time when this cooldown ends.
        /// </summary>
        /// <remarks>
        ///     If null, no cooldown is displayed.
        /// </remarks>
        [ViewVariables]
        public TimeSpan? CooldownEnd
        {
            get => _cooldownEnd;
            set
            {
                _cooldownEnd = value;
                Dirty();
            }
        }

        /// <summary>
        ///     The time when this cooldown started.
        /// </summary>
        /// <remarks>
        ///     If null, no cooldown is displayed.
        /// </remarks>
        [ViewVariables]
        public TimeSpan? CooldownStart
        {
            get => _cooldownStart;
            set
            {
                _cooldownStart = value;
                Dirty();
            }
        }

        public override ComponentState GetComponentState()
        {
            return new ItemCooldownComponentState
            {
                CooldownEnd = CooldownEnd,
                CooldownStart = CooldownStart
            };
        }

        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
        {
            base.HandleComponentState(curState, nextState);

            if (curState is not ItemCooldownComponentState cast)
                return;

            CooldownStart = cast.CooldownStart;
            CooldownEnd = cast.CooldownEnd;
        }

        [Serializable, NetSerializable]
        private sealed class ItemCooldownComponentState : ComponentState
        {
            public TimeSpan? CooldownStart { get; set; }
            public TimeSpan? CooldownEnd { get; set; }

            public ItemCooldownComponentState() {
            }
        }
    }
}
