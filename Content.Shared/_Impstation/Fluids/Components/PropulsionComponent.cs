using Content.Shared.Whitelist;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._Impstation.Fluids.Components;

/// <summary>
/// This object will speed up the movement speed of entities
/// when collided with
///
/// Used for mucin
///
/// This partially replicates SpeedModifierContactsComponent because that
/// component is already heavily coupled with existing puddle code.
/// </summary>
public abstract partial class SharedPropulsionComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float WalkSpeedModifier = 1.0f;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float SprintSpeedModifier = 1.0f;

    /// <summary>
    /// If an entity passes this, apply the speed modifier.
    /// Passes all entities if not defined.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public EntityWhitelist? Whitelist;

    /// <summary>
    /// Used to populate <seealso cref="PropulsedByState.PredictFingerprint"/> and
    /// to make client prediction work.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public byte PredictIndex = 0;
}

[Serializable, NetSerializable]
public sealed partial class PropulsionState : ComponentState
{
    public PropulsionState(SharedPropulsionComponent comp)
    {
        WalkSpeedModifier = comp.WalkSpeedModifier;
        SprintSpeedModifier = comp.SprintSpeedModifier;
        PredictIndex = comp.PredictIndex;
        Whitelist = comp.Whitelist;
    }

    public float WalkSpeedModifier;
    public float SprintSpeedModifier;
    public byte PredictIndex;
    public EntityWhitelist? Whitelist;
}
