using Robust.Shared.Serialization;

namespace Content.Shared.CartridgeLoader.Cartridges;

public interface INanoTaskUiMessagePayload
{
}

[Serializable, NetSerializable, DataRecord]
public sealed class NanoTaskAddTask : INanoTaskUiMessagePayload
{
    public readonly NanoTaskItem Item;

    public NanoTaskAddTask(NanoTaskItem item)
    {
        Item = item;
    }
}

[Serializable, NetSerializable, DataRecord]
public sealed class NanoTaskUpdateTask : INanoTaskUiMessagePayload
{
    public readonly NanoTaskItemAndId Item;

    public NanoTaskUpdateTask(NanoTaskItemAndId item)
    {
        Item = item;
    }
}

[Serializable, NetSerializable, DataRecord]
public sealed class NanoTaskDeleteTask : INanoTaskUiMessagePayload
{
    public readonly int Id;

    public NanoTaskDeleteTask(int id)
    {
        Id = id;
    }
}

[Serializable, NetSerializable, DataRecord]
public sealed class NanoTaskPrintTask : INanoTaskUiMessagePayload
{
    public readonly NanoTaskItem Item;

    public NanoTaskPrintTask(NanoTaskItem item)
    {
        Item = item;
    }
}

[Serializable, NetSerializable]
public sealed class NanoTaskUiMessageEvent : CartridgeMessageEvent
{
    public readonly INanoTaskUiMessagePayload Payload;
    public NanoTaskUiMessageEvent(INanoTaskUiMessagePayload payload)
    {
        Payload = payload;
    }
}
