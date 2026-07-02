using Robust.Shared.Serialization;

namespace Content.Shared.Kitchen;

/// <summary>
/// Sent by the client if they want to toggle the grinder's auto mode.
/// </summary>
[Serializable, NetSerializable]
public sealed class ReagentGrinderToggleAutoModeMessage() : BoundUserInterfaceMessage;

/// <summary>
/// Sent by the client if they want to start the grinder.
/// </summary>
[Serializable, NetSerializable]
public sealed class ReagentGrinderStartMessage(GrinderProgram program) : BoundUserInterfaceMessage
{
    public GrinderProgram Program = program;
}

/// <summary>
/// Sent by the client if they want to eject all grindable entities within the grinder.
/// </summary>
[Serializable, NetSerializable]
public sealed class ReagentGrinderEjectChamberAllMessage() : BoundUserInterfaceMessage;

/// <summary>
/// Sent by the client if they want eject a single grindable entity within the grinder.
/// </summary>
[Serializable, NetSerializable]
public sealed class ReagentGrinderEjectChamberContentMessage(NetEntity entityId) : BoundUserInterfaceMessage
{
    public NetEntity EntityId = entityId;
}

/// <summary>
/// Enum to be used for the grinder's appearance data.
/// </summary>
[Serializable, NetSerializable]
public enum ReagentGrinderVisualState : byte
{
    BeakerAttached
}

/// <summary>
/// The mode the grinder will use when activated. Grinding and juicing the same prototype will yield different results.
/// </summary>
[Serializable, NetSerializable]
public enum GrinderProgram : byte
{
    Grind,
    Juice
}

/// <summary>
/// Key for the ReagentGrinderBoundUserInterface.
/// </summary>
[NetSerializable, Serializable]
public enum ReagentGrinderUiKey : byte
{
    Key
}

/// <summary>
/// The setting of the grinder's auto mode.
/// </summary>
[NetSerializable, Serializable]
public enum GrinderAutoMode : byte
{
    Off,
    Grind,
    Juice
}
