using Content.Shared.Light.EntitySystems;
using Content.Shared.Stacks;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Light.Components;


/// <summary>
/// Component that represents a handheld expendable light which can be activated and eventually dies over time.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class ExpendableLightComponent : Component
{

    [ViewVariables(VVAccess.ReadOnly)]
    [AutoNetworkedField]
    public ExpendableLightState CurrentState = ExpendableLightState.Unlit;

    [DataField]
    public string TurnOnBehaviourID = string.Empty;

    [DataField]
    public string FadeOutBehaviourID = string.Empty;

    /// <summary>
    /// How long light will spend in fully glowing state when it's activated. After this time it will start fading out.
    /// Warning: It should only be used in the Unlit state, it does not update in other states. Use <see cref="FadeOutDuration"/> if you need to know when change to next state happens.
    /// </summary>
    [DataField]
    [AutoNetworkedField]
    public TimeSpan GlowDuration = TimeSpan.FromMinutes(15);

    /// <summary>
    /// How long light will spend in fading out state
    /// </summary>
    [DataField]
    public TimeSpan FadeOutDuration = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Material that can be used to refuel the light.
    /// </summary>
    [DataField]
    public ProtoId<StackPrototype>? RefuelMaterialID;

    /// <summary>
    /// How much glow time refueling is restoring.
    /// </summary>
    [DataField]
    public TimeSpan RefuelMaterialTime = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Maximum glow time refueld light can hold.
    /// </summary>
    [DataField]
    public TimeSpan RefuelMaximumDuration = TimeSpan.FromMinutes(20);

    /// <summary>
    /// Sound made by expandable light when they are lit.
    /// </summary>
    [DataField]
    public SoundSpecifier? LitSound;

    /// <summary>
    /// Sound made continuously by expandable light, lopped while light is lit.
    /// </summary>
    [DataField]
    public SoundSpecifier? LoopedSound;

    /// <summary>
    /// Sound made by expandable light when light dies out.
    /// </summary>
    [DataField]
    public SoundSpecifier? DieSound;

    /// <summary>
    /// The icon state used by expendable lights when the they have been completely expended.
    /// </summary>
    [DataField]
    public string? IconStateSpent;

    /// <summary>
    /// The icon state used by expendable lights while they are lit.
    /// </summary>
    [DataField]
    public string? IconStateLit;

    /// <summary>
    /// The sprite layer shader used while the expendable light is lit.
    /// </summary>
    [DataField]
    public string? SpriteShaderLit = null;

    /// <summary>
    /// The sprite layer shader used after the expendable light has burnt out.
    /// </summary>
    [DataField]
    public string? SpriteShaderSpent = null;

    /// <summary>
    /// The color emited by expendable lights while they are lit.
    /// </summary>
    [DataField]
    public Color? GlowColorLit = null;

    /// <summary>
    /// The sound that plays when the expendable light is lit.
    /// </summary>
    [Access(typeof(ExpendableLightSystem))]
    public EntityUid? PlayingStream;

    /// <summary>
    ///     Status of light, whether or not it is emitting light.
    /// </summary>
    [ViewVariables]
    public bool Activated => CurrentState is ExpendableLightState.Lit or ExpendableLightState.Fading;

    /// <summary>
    ///     Time when next change of CurrentState happens. It's current time + how long light will spend in current state.
    /// </summary>
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
    Unlit,
    Lit,
    Fading,
    Dead
}
