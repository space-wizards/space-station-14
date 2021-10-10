using System;
using Content.Shared.Movement.Components;
using Content.Shared.Sound;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Shared.Stunnable
{
    [Friend(typeof(SharedStunSystem))]
    [RegisterComponent, NetworkedComponent]
    public sealed class StunnableComponent : Component, IMoveSpeedModifier
    {
        public sealed override string Name => "Stunnable";

        public (TimeSpan Start, TimeSpan End)? StunnedTimer { get; set; }
        public (TimeSpan Start, TimeSpan End)? KnockdownTimer { get; set; }
        public (TimeSpan Start, TimeSpan End)? SlowdownTimer { get; set; }

        [ViewVariables]
        public float StunnedSeconds =>
            StunnedTimer == null ? 0f : (float)(StunnedTimer.Value.End - StunnedTimer.Value.Start).TotalSeconds;

        [ViewVariables]
        public float KnockdownSeconds =>
            KnockdownTimer == null ? 0f : (float)(KnockdownTimer.Value.End - KnockdownTimer.Value.Start).TotalSeconds;

        [ViewVariables]
        public float SlowdownSeconds =>
            SlowdownTimer == null ? 0f : (float)(SlowdownTimer.Value.End - SlowdownTimer.Value.Start).TotalSeconds;

        [ViewVariables]
        public bool AnyStunActive => Stunned || KnockedDown || SlowedDown;

        [ViewVariables]
        public bool Stunned => StunnedTimer != null;

        [ViewVariables]
        public bool KnockedDown => KnockdownTimer != null;

        [ViewVariables]
        public bool SlowedDown => SlowdownTimer != null;

        [DataField("stunCap")]
        public float StunCap { get; set; } = 20f;

        [DataField("knockdownCap")]
        public float KnockdownCap { get; set; } = 20f;

        [DataField("slowdownCap")]
        public float SlowdownCap { get; set; } = 20f;

        [DataField("helpInterval")]
        public float HelpInterval { get; set; } = 1f;

        [DataField("stunAttemptSound")]
        public SoundSpecifier StunAttemptSound = new SoundPathSpecifier("/Audio/Effects/thudswoosh.ogg");

        [ViewVariables]
        public float HelpTimer { get; set; } = 0f;

        [ViewVariables]
        public float WalkSpeedMultiplier { get; set; } = 0f;
        [ViewVariables]
        public float RunSpeedMultiplier { get; set; } = 0f;

        [ViewVariables]
        public float WalkSpeedModifier => (SlowedDown ? (WalkSpeedMultiplier <= 0f ? 0.5f : WalkSpeedMultiplier) : 1f);
        [ViewVariables]
        public float SprintSpeedModifier => (SlowedDown ? (RunSpeedMultiplier <= 0f ? 0.5f : RunSpeedMultiplier) : 1f);
    }

    [Serializable, NetSerializable]
    public sealed class StunnableComponentState : ComponentState
    {
        public (TimeSpan Start, TimeSpan End)? StunnedTimer { get; }
        public (TimeSpan Start, TimeSpan End)? KnockdownTimer { get; }
        public (TimeSpan Start, TimeSpan End)? SlowdownTimer { get; }
        public float WalkSpeedMultiplier { get; }
        public float RunSpeedMultiplier { get; }

        public StunnableComponentState(
            (TimeSpan Start, TimeSpan End)? stunnedTimer, (TimeSpan Start, TimeSpan End)? knockdownTimer,
            (TimeSpan Start, TimeSpan End)? slowdownTimer, float walkSpeedMultiplier, float runSpeedMultiplier)
        {
            StunnedTimer = stunnedTimer;
            KnockdownTimer = knockdownTimer;
            SlowdownTimer = slowdownTimer;
            WalkSpeedMultiplier = walkSpeedMultiplier;
            RunSpeedMultiplier = runSpeedMultiplier;
        }
    }
}
