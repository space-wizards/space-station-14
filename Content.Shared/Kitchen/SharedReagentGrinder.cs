using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Serialization;

namespace Content.Shared.Kitchen;

/// <summary>
/// Sent by the client if they want to toggle the grinder's auto mode.
/// </summary>
[Serializable, NetSerializable]
public sealed class ReagentGrinderToggleAutoModeMessage : BoundUserInterfaceMessage;

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
public sealed class ReagentGrinderEjectChamberAllMessage : BoundUserInterfaceMessage;

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

/// <summary>
/// State for the reagent grinder UI.
/// </summary>
[NetSerializable, Serializable]
public sealed class ReagentGrinderUpdateUserInterfaceState(
    NetEntity[] chamberEntities,
    NetEntity? beaker,
    bool isActive,
    bool isPowered,
    GrinderProgram? program,
    GrinderAutoMode autoMode,
    List<ReagentQuantity> beakerReagents,
    FixedPoint2 currentVolume,
    FixedPoint2 maxVolume)
    : BoundUserInterfaceState
{
    public NetEntity[] ChamberEntities = chamberEntities;
    public NetEntity? Beaker = beaker;
    public bool IsActive = isActive;
    public bool IsPowered = isPowered;
    public GrinderProgram? Program = program;
    public GrinderAutoMode AutoMode = autoMode;
    public List<ReagentQuantity> BeakerReagents = beakerReagents;
    public FixedPoint2 CurrentVolume = currentVolume;
    public FixedPoint2 MaxVolume = maxVolume;
}
