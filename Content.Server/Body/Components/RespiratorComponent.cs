using System;
using System.Collections.Generic;
using Content.Server.Body.Systems;
using Content.Server.Chemistry.ReagentEffects;
using Content.Shared.Atmos;
using Content.Shared.Damage;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Body.Components
{
    [RegisterComponent, Friend(typeof(RespiratorSystem))]
    public class RespiratorComponent : Component
    {
        public override string Name => "Respirator";

        /// <summary>
        ///     Saturation level. Reduced by CycleDelay each tick.
        ///     Can be thought of as 'how many seconds you have until you start suffocating' in this configuration.
        /// </summary>
        [DataField("saturation")]
        public float Saturation = 5.0f;

        /// <summary>
        ///     At what level of saturation will you begin to suffocate?
        /// </summary>
        [DataField("suffocationThreshold")]
        public float SuffocationThreshold = 0.0f;

        [DataField("maxSaturation")]
        public float MaxSaturation = 10.0f;

        [DataField("minSaturation")]
        public float MinSaturation = -10.0f;

        // TODO HYPEROXIA

        /// <summary>
        ///     What volume of gas should be inhaled at once?
        /// </summary>
        [DataField("inhaleAmount")]
        public float InhaleAmount = Atmospherics.BreathVolume;

        [DataField("damage", required: true)]
        [ViewVariables(VVAccess.ReadWrite)]
        public DamageSpecifier Damage = default!;

        [DataField("damageRecovery", required: true)]
        [ViewVariables(VVAccess.ReadWrite)]
        public DamageSpecifier DamageRecovery = default!;

        [DataField("gaspPopupCooldown")]
        public TimeSpan GaspPopupCooldown { get; private set; } = TimeSpan.FromSeconds(8);

        [ViewVariables]
        public TimeSpan LastGaspPopupTime;

        [ViewVariables]
        public bool Suffocating = false;

        [ViewVariables]
        public RespiratorStatus Status = RespiratorStatus.Inhaling;

        [DataField("cycleDelay")]
        public float CycleDelay = 2.0f;

        public float AccumulatedFrametime;
    }
}

public enum RespiratorStatus
{
    Inhaling,
    Exhaling
}
