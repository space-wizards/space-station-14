using Content.Shared.NanoTask.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.CartridgeLoader.Cartridges;

/// <summary>
///     Base UI message for NanoTask interactions
/// </summary>
public interface INanoTaskUiMessagePayload;

[Serializable, NetSerializable]
public enum NanoTaskCategory
{
    Station,
    Department,
}

[Serializable, NetSerializable]
public sealed record NanoTaskCategoryAndDepartment(NanoTaskCategory Category, ProtoId<NanoTaskDepartmentPrototype>? Department);

/// <summary>
///     Dispatched when a new task is created
/// </summary>
[Serializable, NetSerializable, DataRecord]
public sealed class NanoTaskAddTask : INanoTaskUiMessagePayload
{
    /// <summary>
    ///     The newly created task
    /// </summary>
    public readonly NanoTaskItem Item;

    public readonly NanoTaskCategoryAndDepartment Category;

    public NanoTaskAddTask(NanoTaskItem item, NanoTaskCategoryAndDepartment category)
    {
        Item = item;
        Category = category;
    }
}

/// <summary>
///     Dispatched when an existing task is modified
/// </summary>
[Serializable, NetSerializable, DataRecord]
public sealed class NanoTaskUpdateTask(NanoTaskItemAndDepartment item) : INanoTaskUiMessagePayload
{
    /// <summary>
    ///     The task that was updated and its ID
    /// </summary>
    public readonly NanoTaskItemAndDepartment Item = item;
}

/// <summary>
///     Dispatched when an existing task is deleted
/// </summary>
[Serializable, NetSerializable, DataRecord]
public sealed class NanoTaskDeleteTask : INanoTaskUiMessagePayload
{
    /// <summary>
    ///     The ID of the task to delete
    /// </summary>
    public readonly uint Id;

    public NanoTaskDeleteTask(uint id)
    {
        Id = id;
    }
}

/// <summary>
///     Cartridge message event carrying the NanoTask UI messages
/// </summary>
[Serializable, NetSerializable]
public sealed class NanoTaskUiMessageEvent : CartridgeMessageEvent
{
    public readonly INanoTaskUiMessagePayload Payload;

    public NanoTaskUiMessageEvent(INanoTaskUiMessagePayload payload)
    {
        Payload = payload;
    }
}
