using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Silicons.StationAi;

/// <summary>
/// This component holds data needed for AI Restoration Consoles to function.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(SharedStationAiFixerConsoleSystem))]
public sealed partial class StationAiFixerConsoleComponent : Component
{
    /// <summary>
    /// Determines how long a repair takes to complete (in seconds).
    /// </summary>
    [DataField]
    public TimeSpan RepairDuration = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Determines how long a purge takes to complete (in seconds).
    /// </summary>
    [DataField]
    public TimeSpan PurgeDuration = TimeSpan.FromSeconds(30);

    /// <summary>
    /// The number of stages that a console action (repair or purge)
    /// progresses through before it concludes. Each stage has an equal
    /// duration. The appearance data of the entity is updated with
    /// each new stage reached.
    /// </summary>
    [DataField]
    public int ActionStageCount = 4;

    /// <summary>
    /// The time at which the current action commenced.
    /// </summary>
    [DataField, AutoNetworkedField, AutoPausedField]
    public TimeSpan ActionStartTime = TimeSpan.FromSeconds(0);

    /// <summary>
    /// The time at which the current action will end.
    /// </summary>
    [DataField, AutoNetworkedField, AutoPausedField]
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
    public int CurrentActionStage;

    /// <summary>
    /// Sound clip that is played when a repair is completed.
    /// </summary>
    [DataField]
    public SoundSpecifier? RepairFinishedSound = new SoundPathSpecifier("/Audio/Items/beep.ogg");

    /// <summary>
    /// Sound clip that is played when a repair is completed.
    /// </summary>
    [DataField]
    public SoundSpecifier? PurgeFinishedSound = new SoundPathSpecifier("/Audio/Machines/beep.ogg");

    /// <summary>
    /// The name of the console slot which is used to contain station AI holders.
    /// </summary>
    [DataField]
    public string StationAiHolderSlot = "station_ai_holder";

    /// <summary>
    /// The name of the station AI holder slot which actually contains the station AI.
    /// </summary>
    [DataField]
    public string StationAiMindSlot = "station_ai_mind_slot";
}

/// <summary>
/// Message sent from the server to the client to update the UI of AI Restoration Consoles.
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

