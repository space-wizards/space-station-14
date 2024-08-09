using Content.Shared.Damage;
using Content.Shared.NPC.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Clothing.Components;

/// <summary>
/// This is used for a mask that takes over the host when worn.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedCursedMaskSystem))]
public sealed partial class CursedMaskComponent : Component
{
    /// <summary>
    /// The current expression shown. Used to determine which effect is applied.
    /// </summary>
    [DataField]
    public CursedMaskExpression CurrentState = CursedMaskExpression.Neutral;

    /// <summary>
    /// Speed modifier applied when the "Joy" expression is present.
    /// </summary>
    [DataField]
    public float JoySpeedModifier = 1.15f;

    /// <summary>
    /// Damage modifier applied when the "Despair" expression is present.
    /// </summary>
    [DataField]
    public DamageModifierSet DespairDamageModifier = new();

    /// <summary>
    /// Whether or not the mask is currently attached to an NPC.
    /// </summary>
    [DataField]
    public bool HasNpc;

    /// <summary>
    /// The mind that was booted from the wearer when the mask took over.
    /// </summary>
    [DataField]
    public EntityUid? StolenMind;

    [DataField]
    public ProtoId<NpcFactionPrototype> CursedMaskFaction = "SimpleHostile";

    [DataField]
    public HashSet<ProtoId<NpcFactionPrototype>> OldFactions = new();
}

[Serializable, NetSerializable]
public enum CursedMaskVisuals : byte
{
     State
}

[Serializable, NetSerializable]
public enum CursedMaskExpression : byte
{
    Neutral,
    Joy,
    Despair,
    Anger
}
