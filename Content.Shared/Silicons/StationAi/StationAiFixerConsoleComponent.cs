using Content.Shared.Damage.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using System.Numerics;

namespace Content.Shared.Silicons.StationAi;

/// <summary>
/// This components holds data for AI Restoration Consoles.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(StationAiFixerConsoleSystem))]
public sealed partial class StationAiFixerConsoleComponent : Component
{
    /// <summary>
    /// Sets how much damage can be removed per second by this entity.
    /// This is used to determine how long a repair will take to complete.
    /// Note that damage removal is only applied to a target once
    /// <see cref="ActionEndTime"/> has been reached.
    /// </summary>
    [DataField]
    public float RepairRate = 1.6667f;

    /// <summary>
    /// Sets how much damage is inflicted per second by this entity.
    /// This is used to determine how long a purge will take to complete.
    /// Note that damage is only applied to a target once
    /// <see cref="ActionEndTime"/> has been reached.
    /// </summary>
    [DataField]
    public float PurgeRate = 10f;

    /// <summary>
    /// The number of stages that an action progresses through
    /// before it concludes. Each stage has an equal duration.
    /// The appearance data of the entity will be updated with
    /// each new stage reached.
    /// </summary>
    [DataField]
    public int ActionStageCount = 4;

    /// <summary>
    /// Sets the minimum and maximum amount of time (in seconds)
    /// that this entity requires to perform an action.
    /// </summary>
    [DataField]
    public Vector2 ActionTimeLimits = new Vector2(10f, 600f);

    /// <summary>
    /// Sets the damage type to be used when purging a station AI.
    /// </summary>
    [DataField]
    public ProtoId<DamageTypePrototype> PurgeDamageType = "Shock";

    /// <summary>
    /// The time at which the current action commenced.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan ActionStartTime = TimeSpan.FromSeconds(0);

    /// <summary>
    /// The time at which the current action will end.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan ActionEndTime = TimeSpan.FromSeconds(0);

    /// <summary>
    /// The type of action that is currently in progress.
    /// </summary>
    [DataField, AutoNetworkedField]
    public StationAiFixerConsoleAction ActionType = StationAiFixerConsoleAction.None;

    /// <summary>
    /// The target of the current action.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? ActionTarget;

    /// <summary>
    /// The current stage of the action in progress.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int CurrentActionStage = 0;
}

/// <summary>
/// Used to trigger UI updates for AI Restoration Consoles.
/// </summary>
[Serializable, NetSerializable]
public sealed class StationAiFixerConsoleBoundUserInterfaceState : BoundUserInterfaceState;

/// <summary>
/// Message sent from the client to the server to handle player UI inputs from AI Restoration Consoles.
/// </summary>
[Serializable, NetSerializable]
public sealed class StationAiFixerConsoleMessage : BoundUserInterfaceMessage
{
    public StationAiFixerConsoleAction Action;

    public StationAiFixerConsoleMessage(StationAiFixerConsoleAction action)
    {
        Action = action;
    }
}

/// <summary>
/// Potential actions that AI Restoration Consoles can perform.
/// </summary>
[Serializable, NetSerializable]
public enum StationAiFixerConsoleAction
{
    None,
    Eject,
    Repair,
    Purge,
    Cancel,
}

/// <summary>
/// Appearance keys for AI Restoration Consoles.
/// </summary>
[Serializable, NetSerializable]
public enum StationAiFixerConsoleVisuals : byte
{
    Key,
    ActionProgress,
    MobState,
    RepairProgress,
    PurgeProgress,
}

/// <summary>
/// Interactable UI key for AI Restoration Consoles.
/// </summary>
[Serializable, NetSerializable]
public enum StationAiFixerConsoleUiKey
{
    Key,
}

