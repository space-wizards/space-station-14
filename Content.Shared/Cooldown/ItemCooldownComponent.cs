using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Cooldown
{
    /// <summary>
    ///     Stores a visual "cooldown" for items, that gets displayed in the hands GUI.
    /// </summary>
    [RegisterComponent, NetworkedComponent]
    [AutoGenerateComponentState]
    public sealed partial class ItemCooldownComponent : Component
    {
        // TODO: access and system setting and dirtying not this funny stuff
        private TimeSpan? _cooldownEnd;
        private TimeSpan? _cooldownStart;

        /// <summary>
        ///     The time when this cooldown ends.
        /// </summary>
        /// <remarks>
        ///     If null, no cooldown is displayed.
        /// </remarks>
        [ViewVariables, AutoNetworkedField]
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
        [ViewVariables, AutoNetworkedField]
        public TimeSpan? CooldownStart
        {
            get => _cooldownStart;
            set
            {
                _cooldownStart = value;
                Dirty();
            }
        }
    }
}
