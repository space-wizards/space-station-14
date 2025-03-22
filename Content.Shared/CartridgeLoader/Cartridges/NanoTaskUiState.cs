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
    public readonly bool IsTaskDone;

    /// <summary>
    ///     The task's marked priority
    /// </summary>
    public readonly NanoTaskPriority Priority;

    public NanoTaskItem(string description, string taskIsFor, bool isTaskDone, NanoTaskPriority priority)
    {
        Description = description;
        TaskIsFor = taskIsFor;
        IsTaskDone = isTaskDone;
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
    public readonly int Id;
    public readonly NanoTaskItem Data;

    public NanoTaskItemAndId(int id, NanoTaskItem data)
    {
        Id = id;
        Data = data;
    }
};

/// <summary>
///     The UI state of the NanoTask
/// </summary>
[Serializable, NetSerializable]
public sealed class NanoTaskUiState : BoundUserInterfaceState
{
    public List<NanoTaskItemAndId> Tasks;

    public NanoTaskUiState(List<NanoTaskItemAndId> tasks)
    {
        Tasks = tasks;
    }
}
