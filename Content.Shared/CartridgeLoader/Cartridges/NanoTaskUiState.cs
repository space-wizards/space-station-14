using Robust.Shared.Serialization;

namespace Content.Shared.CartridgeLoader.Cartridges;

[Serializable, NetSerializable]
public enum NanoTaskPriority
{
    High,
    Medium,
    Low,
};

[Serializable, NetSerializable, DataRecord]
public sealed class NanoTaskItem
{
    public static int MaximumStringLength = 30;
    public readonly string Description;
    public readonly string TaskIsFor;
    public readonly bool IsTaskDone;
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

[Serializable, NetSerializable]
public sealed class NanoTaskUiState : BoundUserInterfaceState
{
    public List<NanoTaskItemAndId> Tasks;

    public NanoTaskUiState(List<NanoTaskItemAndId> tasks)
    {
        Tasks = tasks;
    }
}
