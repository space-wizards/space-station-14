using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared.Medical.Consciousness.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState()]
public sealed class ConsciousnessComponent : Component
{
    //Unconsciousness threshold, ie: when does this entity pass out
    [DataField("threshold", required: true), AutoNetworkedField()]
    public FixedPoint2 Threshold = 30;

    //The raw/starting consciousness value, this should be between 0 and the cap or -1 to automatically get the cap
    // Do not directly edit this value, use modifiers instead!
    [DataField("consciousness"), AutoNetworkedField()]
    public FixedPoint2 RawConsciousness = -1;

    //The current consciousness value adjusted by the multiplier and clamped
    [AutoNetworkedField()] public FixedPoint2 Consciousness => FixedPoint2.Clamp(RawConsciousness * Multiplier, 0, Cap);

    //Multiplies the consciousness value whenever it is used. Do not directly edit this value, use multipliers instead!
    [DataField("multiplier"), AutoNetworkedField()]
    public FixedPoint2 Multiplier = 1.0;

    //The maximum consciousness value, and starting consciousness if rawConsciousness is -1
    [DataField("cap"), AutoNetworkedField()]
    public FixedPoint2 Cap = 100;

    //List of modifiers that are applied to this consciousness
    [DataField("modifiers"), AutoNetworkedField()]
    public Dictionary<EntityUid,ConsciousnessModifier> Modifiers = new();

    //List of multipliers that are applied to this consciousness
    [DataField("multipliers"), AutoNetworkedField()]
    public Dictionary<EntityUid,ConsciousnessMultiplier> Multipliers = new();
}

[Serializable, DataRecord]
public record struct ConsciousnessModifier(FixedPoint2 Change, string Identifier = "Unspecified");

[Serializable, DataRecord]
public record struct ConsciousnessMultiplier(FixedPoint2 Multiplier,
    string Identifier = "Unspecified");
