using System;
using System.Collections.Generic;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared.StatusEffect
{
    [RegisterComponent]
    [NetworkedComponent]
    [Friend(typeof(StatusEffectsSystem))]
    public class StatusEffectsComponent : Component
    {
        public override string Name => "StatusEffects";

        public Dictionary<string, StatusEffectState> ActiveEffects = new();

        /// <summary>
        ///     A list of status effect IDs to be allowed
        /// </summary>
        [DataField("allowed", required: true)]
        public List<string> AllowedEffects = default!;
    }

    /// <summary>
    ///     Holds information about an active status effect.
    /// </summary>
    [Serializable, NetSerializable]
    public class StatusEffectState
    {
        /// <summary>
        ///     The start and end times of the status effect.
        /// </summary>
        public (TimeSpan, TimeSpan) Cooldown;

        /// <summary>
        ///     The name of the relevant component that
        ///     was added alongside the effect, if any.
        /// </summary>
        public string? RelevantComponent;

        public StatusEffectState((TimeSpan, TimeSpan) cooldown, string? relevantComponent=null)
        {
            Cooldown = cooldown;
            RelevantComponent = relevantComponent;
        }
    }

    [Serializable, NetSerializable]
    public class StatusEffectsComponentState : ComponentState
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
