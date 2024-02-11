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
    [DataField(required: true), AutoNetworkedField]
    public FixedPoint2 Threshold = 30;

    /// <summary>
    /// The current unmodified consciousness value, if this is below the threshold the entity is in crit and
    /// when this value reaches 0 the entity is dead.
    /// Do not use this directly, use GetConsciousness on the system instead as it properly applies mults/mods and clamps.
    /// </summary>
    [DataField("Consciousness"), AutoNetworkedField]
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
    [DataField, AutoNetworkedField]
    public FixedPoint2 Cap = MaxConsciousness;

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
    /// Brain that is hosting this consciousness
    /// </summary>
    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadOnly)]
    public EntityUid? LinkedBrain;

    public const float MaxConsciousness = 100f;
}
