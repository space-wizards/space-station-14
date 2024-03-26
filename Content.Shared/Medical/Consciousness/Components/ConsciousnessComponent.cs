using Content.Shared.FixedPoint;
using Content.Shared.Medical.Consciousness.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.Medical.Consciousness.Components;



[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(ConsciousnessSystem))]
public sealed partial class ConsciousnessComponent : Component
{
    /// <summary>
    /// Unconsciousness threshold, ie: when does this entity pass-out/enter-crit
    /// </summary>
    [DataField("threshold",required: true), AutoNetworkedField]
    public FixedPoint2 RawThreshold = 30;

    /// <summary>
    /// The current unmodified consciousness value, if this is below the threshold the entity is in crit and
    /// when this value reaches 0 the entity is dead.
    /// </summary>
    [DataField("consciousness"), AutoNetworkedField]
    public FixedPoint2 RawValue = MaxConsciousness;

    /// <summary>
    /// The current multiplier for consciousness.
    /// This value is used to multiple RawConsciousness before modifiers are applied.
    /// </summary>
    [DataField, AutoNetworkedField]
    public FixedPoint2 Multiplier = 1.0;

    /// <summary>
    /// The current modifier for consciousness.
    /// This value is added after raw consciousness is multiplied by the multiplier.
    /// </summary>
    [DataField, AutoNetworkedField]
    public FixedPoint2 Modifier = 0;

    /// <summary>
    /// The current maximum consciousness value, consciousness is clamped with this value.
    /// </summary>
    [DataField("Cap"), AutoNetworkedField]
    public FixedPoint2 RawCap = MaxConsciousness;

    /// <summary>
    /// Is consciousness being prevented from changing mobstate
    /// </summary>
    [DataField,AutoNetworkedField, ViewVariables(VVAccess.ReadOnly)]
    public bool OverridenByMobstate = false;

    /// <summary>
    /// Is this entity currently conscious
    /// </summary>
    [AutoNetworkedField, ViewVariables(VVAccess.ReadOnly)]
    public bool IsConscious = true;

    /// <summary>
    /// How many consciousness providers do we expect to be fully functioning,
    /// each removed provider will decrease consciousness by 1/ExpectedProviderCount * 100
    /// </summary>
    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadOnly)]
    public int ExpectedProviderCount = 1;

    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadOnly)]
    public List<EntityUid> LinkedProviders = new();

    public FixedPoint2 Consciousness => FixedPoint2.Clamp(RawValue * Multiplier + Modifier, 0, Cap);
    public FixedPoint2 Cap => FixedPoint2.Clamp(RawCap, 0, MaxConsciousness);

    public const float MaxConsciousness = 100f;
}
