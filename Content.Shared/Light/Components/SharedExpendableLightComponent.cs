using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Light.Components;

[NetworkedComponent]
public abstract partial class SharedExpendableLightComponent : Component
{

    [ViewVariables(VVAccess.ReadOnly)]
    public ExpendableLightState CurrentState { get; set; }

    [DataField("turnOnBehaviourID")]
    public string TurnOnBehaviourID { get; set; } = string.Empty;

    [DataField("fadeOutBehaviourID")]
    public string FadeOutBehaviourID { get; set; } = string.Empty;

    [DataField("glowDuration")]
    public float GlowDuration { get; set; } = 60 * 15f;

    [DataField("fadeOutDuration")]
    public float FadeOutDuration { get; set; } = 60 * 5f;

    [DataField("spentDesc")]
    public string SpentDesc { get; set; } = string.Empty;

    [DataField("spentName")]
    public string SpentName { get; set; } = string.Empty;

    [DataField("litSound")]
    public SoundSpecifier? LitSound { get; set; }

    [DataField("loopedSound")]
    public SoundSpecifier? LoopedSound { get; set; }

    [DataField("dieSound")]
    public SoundSpecifier? DieSound { get; set; } = null;
}

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
