using Robust.Shared.Serialization;

namespace Content.Shared.CartridgeLoader.Cartridges;

/// <summary>
///     The priority assigned to a NanoTask item
/// </summary>
[Serializable, NetSerializable]
public enum NanoTaskPriority : byte
{
    High,
    Medium,
    Low,
};

[Serializable, NetSerializable]
public enum NanoTaskItemStatus : byte
{
    NotStarted,
    InProgress,
    Completed
}

/// <summary>
///     The data relating to a single NanoTask item, but not its identifier
/// </summary>
[Serializable, NetSerializable, DataRecord]
public sealed class NanoTaskItem
{
    /// <summary>
    ///     The maximum length of the Description and TaskIsFor fields
    /// </summary>
    public static int MaximumStringLength = 30;

    /// <summary>
    ///     The task description, i.e. "Bake a cake"
    /// </summary>
    public readonly string Description;

    /// <summary>
    ///     Who the task is for, i.e. "Cargo"
    /// </summary>
    public readonly string TaskIsFor;

    /// <summary>
    ///     If the task is marked as done or not
    /// </summary>
    public readonly NanoTaskItemStatus Status;

    /// <summary>
    ///     The task's marked priority
    /// </summary>
    public readonly NanoTaskPriority Priority;

    public NanoTaskItem(string description, string taskIsFor, NanoTaskItemStatus status, NanoTaskPriority priority)
    {
        Description = description;
        TaskIsFor = taskIsFor;
        Status = status;
        Priority = priority;
    }

    public bool Validate()
    {
        return Description.Length <= MaximumStringLength && TaskIsFor.Length <= MaximumStringLength;
    }
};

/// <summary>
///     Pairs a NanoTask item and its identifier
/// </summary>
[Serializable, NetSerializable, DataRecord]
public sealed class NanoTaskItemAndId
{
    public readonly uint Id;
    public readonly NanoTaskItem Data;

    public NanoTaskItemAndId(uint id, NanoTaskItem data)
    {
        Id = id;
        Data = data;
    }
};

/// <summary>
///     The UI state of the NanoTask
/// </summary>
[Serializable, NetSerializable]
public sealed class NanoTaskUiState(List<(string, NanoTaskItemAndId)> departamentTasks, List<NanoTaskItemAndId> stationTasks) : BoundUserInterfaceState
{
    public List<NanoTaskItemAndId> StationTasks = stationTasks;
    public List<(string, NanoTaskItemAndId)> DepartamentTasks = departamentTasks;
}

[Serializable, NetSerializable]
public sealed class NanoTaskServerOfflineUiState : BoundUserInterfaceState;
