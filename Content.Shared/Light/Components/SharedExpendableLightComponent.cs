using Content.Shared.Light.EntitySystems;
using Content.Shared.Stacks;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Light.Components;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class ExpendableLightComponent : Component
{

    [ViewVariables(VVAccess.ReadOnly)]
    [AutoNetworkedField]
    public ExpendableLightState CurrentState;

    [DataField]
    public string TurnOnBehaviourID = string.Empty;

    [DataField]
    public string FadeOutBehaviourID = string.Empty;

    [DataField]
    public TimeSpan GlowDuration = TimeSpan.FromSeconds(60 * 15f);

    [DataField]
    public TimeSpan FadeOutDuration = TimeSpan.FromSeconds(60 * 5f);

    [DataField]
    public ProtoId<StackPrototype>? RefuelMaterialID;

    [DataField]
    public TimeSpan RefuelMaterialTime = TimeSpan.FromSeconds(15f);

    [DataField]
    public TimeSpan RefuelMaximumDuration = TimeSpan.FromSeconds(60 * 15f * 2);

    [DataField]
    public SoundSpecifier? LitSound;

    [DataField]
    public SoundSpecifier? LoopedSound;

    [DataField]
    public SoundSpecifier? DieSound;

    [Access(typeof(SharedExpendableLightSystem))]
    public EntityUid? PlayingStream;

    [DataField]
    public string? IconStateSpent;
    [DataField]
    public string? IconStateLit;
    [DataField]
    public string? SpriteShaderLit = null;
    [DataField]
    public string? SpriteShaderSpent = null;
    [DataField]
    public Color? GlowColorLit = null;

    [ViewVariables]
    public bool Activated => CurrentState is ExpendableLightState.Lit or ExpendableLightState.Fading;

    [ViewVariables]
    [AutoNetworkedField]
    public TimeSpan? StateExpiryTime;
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
