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

        [ViewVariables(VVAccess.ReadOnly)]
        public ExpendableLightState CurrentState { get; set; }

        [ViewVariables]
        [DataField("turnOnBehaviourID")]
        public string TurnOnBehaviourID { get; set; } = string.Empty;

        [ViewVariables]
        [DataField("fadeOutBehaviourID")]
        public string FadeOutBehaviourID { get; set; } = string.Empty;

        [ViewVariables]
        [DataField("glowDuration")]
        public float GlowDuration { get; set; } = 60 * 15f;

        [ViewVariables]
        [DataField("fadeOutDuration")]
        public float FadeOutDuration { get; set; } = 60 * 5f;

        [ViewVariables]
        [DataField("spentDesc")]
        public string SpentDesc { get; set; } = string.Empty;

        [ViewVariables]
        [DataField("spentName")]
        public string SpentName { get; set; } = string.Empty;

        [ViewVariables]
        [DataField("iconStateSpent")]
        public string IconStateSpent { get; set; } = string.Empty;

        [ViewVariables]
        [DataField("iconStateOn")]
        public string IconStateLit { get; set; } = string.Empty;

        [ViewVariables]
        [DataField("litSound", required: true)]
        public SoundSpecifier LitSound { get; set; } = default!;

        [ViewVariables]
        [DataField("loopedSound")]
        public string? LoopedSound { get; set; } = null;

        [ViewVariables]
        [DataField("dieSound")]
        public SoundSpecifier? DieSound { get; set; } = null;
    }
}
