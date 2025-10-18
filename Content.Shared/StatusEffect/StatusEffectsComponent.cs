using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.StatusEffect
{
    [RegisterComponent]
    [NetworkedComponent]
    [Access(typeof(StatusEffectsSystem))]
    public sealed partial class StatusEffectsComponent : Component
    {
        [ViewVariables]
        public Dictionary<string, StatusEffectState> ActiveEffects = new();

        /// <summary>
        ///     A list of status effect IDs to be allowed
        /// </summary>
        [DataField("allowed", required: true), Access(typeof(StatusEffectsSystem), Other = AccessPermissions.ReadExecute)]
        public List<string> AllowedEffects = default!;
    }

    [RegisterComponent]
    public sealed partial class ActiveStatusEffectsComponent : Component {}

    /// <summary>
    ///     Holds information about an active status effect.
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class StatusEffectState
    {
        /// <summary>
        ///     The start and end times of the status effect.
        /// </summary>
        [ViewVariables]
        public (TimeSpan, TimeSpan) Cooldown;

        /// <summary>
        ///     Specifies whether to refresh or accumulate the cooldown of the status effect.
        ///     true - refresh time, false - accumulate time.
        /// </summary>
        [ViewVariables]
        public bool CooldownRefresh = true;

        /// <summary>
        ///     The name of the relevant component that
        ///     was added alongside the effect, if any.
        /// </summary>
        [ViewVariables]
        public string? RelevantComponent;

        public StatusEffectState((TimeSpan, TimeSpan) cooldown, bool refresh, string? relevantComponent=null)
        {
            Cooldown = cooldown;
            CooldownRefresh = refresh;
            RelevantComponent = relevantComponent;
        }

        public StatusEffectState(StatusEffectState toCopy)
        {
            Cooldown = (toCopy.Cooldown.Item1, toCopy.Cooldown.Item2);
            CooldownRefresh = toCopy.CooldownRefresh;
            RelevantComponent = toCopy.RelevantComponent;
        }
    }

    [Serializable, NetSerializable]
    public sealed class StatusEffectsComponentState : ComponentState
    {
        public Dictionary<string, StatusEffectState> ActiveEffects;
        public List<string> AllowedEffects;

        public StatusEffectsComponentState(Dictionary<string, StatusEffectState> activeEffects, List<string> allowedEffects)
        {
            ActiveEffects = activeEffects;
            AllowedEffects = allowedEffects;
        }
    }
}
