using System;
using Content.Shared.Sound;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Players;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Shared.Light.Component
{
    [Serializable, NetSerializable]
    public enum ExpendableLightVisuals
    {
        State,
        Behavior
    }

    [Serializable, NetSerializable]
    public enum ExpendableLightState
    {
        BrandNew,
        Lit,
        Fading,
        Dead
    }

    [NetworkedComponent]
    public abstract class SharedExpendableLightComponent: Robust.Shared.GameObjects.Component
    {
        public static readonly AudioParams LoopedSoundParams = new(0, 1, "Master", 62.5f, 1, 1, true, 0.3f);

        public sealed override string Name => "ExpendableLight";

        [ViewVariables(VVAccess.ReadOnly)]
        public ExpendableLightState CurrentState { get; set; }

        [ViewVariables]
        [DataField("turnOnBehaviourID")]
        protected string TurnOnBehaviourID { get; set; } = string.Empty;

        [ViewVariables]
        [DataField("fadeOutBehaviourID")]
        protected string FadeOutBehaviourID { get; set; } = string.Empty;

        [ViewVariables]
        [DataField("glowDuration")]
        protected float GlowDuration { get; set; } = 60 * 15f;

        [ViewVariables]
        [DataField("fadeOutDuration")]
        protected float FadeOutDuration { get; set; } = 60 * 5f;

        [ViewVariables]
        [DataField("spentDesc")]
        protected string SpentDesc { get; set; } = string.Empty;

        [ViewVariables]
        [DataField("spentName")]
        protected string SpentName { get; set; } = string.Empty;

        [ViewVariables]
        [DataField("iconStateSpent")]
        protected string IconStateSpent { get; set; } = string.Empty;

        [ViewVariables]
        [DataField("iconStateOn")]
        protected string IconStateLit { get; set; } = string.Empty;

        [ViewVariables]
        [DataField("litSound", required: true)]
        protected SoundSpecifier LitSound { get; set; } = default!;

        [ViewVariables]
        [DataField("loopedSound")]
        public string? LoopedSound { get; set; } = null;

        [ViewVariables]
        [DataField("dieSound")]
        protected SoundSpecifier? DieSound { get; set; } = null;
    }
}
