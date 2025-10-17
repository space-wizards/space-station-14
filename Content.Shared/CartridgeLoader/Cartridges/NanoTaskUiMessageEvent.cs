using Robust.Shared.Serialization;

namespace Content.Shared.CartridgeLoader.Cartridges;

/// <summary>
///     Base UI message for NanoTask interactions
/// </summary>
public interface INanoTaskUiMessagePayload
{
}

/// <summary>
///     Dispatched when a new task is created
/// </summary>
[Serializable, NetSerializable, DataRecord]
public sealed partial class NanoTaskAddTask : INanoTaskUiMessagePayload
{
    /// <summary>
    ///     The newly created task
    /// </summary>
    public readonly NanoTaskItem Item;

    public NanoTaskAddTask(NanoTaskItem item)
    {
        Item = item;
    }
}

/// <summary>
///     Dispatched when an existing task is modified
/// </summary>
[Serializable, NetSerializable, DataRecord]
public sealed partial class NanoTaskUpdateTask : INanoTaskUiMessagePayload
{
    /// <summary>
    ///     The task that was updated and its ID
    /// </summary>
    public readonly NanoTaskItemAndId Item;

    public NanoTaskUpdateTask(NanoTaskItemAndId item)
    {
        Item = item;
    }
}

/// <summary>
///     Dispatched when an existing task is deleted
/// </summary>
[Serializable, NetSerializable, DataRecord]
public sealed partial class NanoTaskDeleteTask : INanoTaskUiMessagePayload
{
    /// <summary>
    ///     The ID of the task to delete
    /// </summary>
    public readonly int Id;

    public NanoTaskDeleteTask(int id)
    {
        Id = id;
    }
}

/// <summary>
///     Dispatched when a task is requested to be printed
/// </summary>
[Serializable, NetSerializable, DataRecord]
public sealed partial class NanoTaskPrintTask : INanoTaskUiMessagePayload
{
    /// <summary>
    ///     The NanoTask to print
    /// </summary>
    public readonly NanoTaskItem Item;

    public NanoTaskPrintTask(NanoTaskItem item)
    {
        Item = item;
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
