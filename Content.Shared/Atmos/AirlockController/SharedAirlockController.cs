using Robust.Shared.Serialization;

namespace Content.Shared.Atmos.AirlockController;

/// <summary>
///     Which end of the airlock does this thing belong to?
/// </summary>
[Serializable, NetSerializable]
public enum AirlockSide : byte
{
    A = 0,
    B = 1,
}

/// <summary>
///     State Machine states. Error and cancel are flags, we just use them to state switch
/// </summary>
[Serializable, NetSerializable]
public enum AirlockCycleState : byte
{
    /// <summary>
    ///     Standard state, we expect the gas mix to be equal to the current side
    /// </summary>
    Idle = 0,

    /// <summary>
    ///     Closing and bolting both doors, but waiting for confirmation before moving on
    /// </summary>
    Sealing = 1,

    /// <summary>
    ///     Siphoning the chamber empty, to whichever side we are currently on.
    /// </summary>
    Evacuating = 2,

    /// <summary>
    ///     Injecting until the side we're currently on is matching pressure
    /// </summary>
    Filling = 3,

    /// <summary>
    ///     We're done, so we're releasing bolts. If emergency unbolt, may mean there's still pressure difference.
    ///     But if that's the case that's your problem mr. impatient tider, hope space-wind takes you.
    /// </summary>
    Unsealing = 4,
}

/// <summary>
///     What a linked vent is allowed to do, per side. Can be multiple (Siphon only siphons, vents can either)
///     Airlock states use this info to decide which device to use.
/// </summary>
[Flags]
[Serializable, NetSerializable]
public enum AirlockVentRole : byte
{
    None = 0,
    VentA = 1 << 0,
    SiphonA = 1 << 1,
    VentB = 1 << 2,
    SiphonB = 1 << 3,
}

/// <summary>
///     Error reporting for UI, art, or whatever. Also for the atmos tech to figure out what they did wrong.
/// </summary>
[Serializable, NetSerializable]
public enum AirlockStallReason : byte
{
    /// <summary>
    ///     No vent is configured for the direction the current state needs.
    /// </summary>
    NoUsableVent = 0,

    /// <summary>
    ///     No sensor to check progress so can't control.
    /// </summary>
    NoSensors = 1,

    /// <summary>
    ///     Things are running but nothing is changing. Empty distro, spaced room, blocked vents..
    /// </summary>
    NotProgressing = 2,

    /// <summary>
    ///     A door opened or broke mid-cycle so we cry about it.
    /// </summary>
    SealLost = 3,

    /// <summary>
    ///     The doors aren't closing or bolting. Restore power or gett that cabinet off the door
    /// </summary>
    NotSealing = 4,

    /// <summary>
    ///     Not enough of an airlock to cycle. Needs a door assigned to each side.
    /// </summary>
    MissingDoors = 5,

    /// <summary>
    ///     Cycle is done but the target side won't unbolt or open.
    /// </summary>
    NotOpening = 6,
}

[Serializable, NetSerializable]
public enum AirlockControllerWireStatus : byte
{
    BoltingIndicator,
    EmergencyLightIndicator,
}

/// <summary>
///     What a controller or one of its panels is currently showing the player.
/// </summary>
[Serializable, NetSerializable]
public readonly record struct AirlockCycleStatus(
    AirlockCycleState State,
    AirlockSide Side,
    AirlockStallReason? Stall,
    bool Maintenance,
    bool Warning);

/// <summary>
///     Shared by the status window and the examine text.
/// </summary>
public static class AirlockControllerLocale
{
    public static string StateKey(AirlockCycleState state) => state switch
    {
        AirlockCycleState.Sealing => "airlock-controller-state-sealing",
        AirlockCycleState.Evacuating => "airlock-controller-state-evacuating",
        AirlockCycleState.Filling => "airlock-controller-state-filling",
        AirlockCycleState.Unsealing => "airlock-controller-state-unsealing",
        _ => "airlock-controller-state-idle",
    };

    public static string SideKey(AirlockSide side) => side == AirlockSide.B
        ? "airlock-controller-side-b"
        : "airlock-controller-side-a";

    public static string StallKey(AirlockStallReason reason) => reason switch
    {
        AirlockStallReason.NoUsableVent => "airlock-controller-stall-no-vent",
        AirlockStallReason.NoSensors => "airlock-controller-stall-no-sensors",
        AirlockStallReason.SealLost => "airlock-controller-stall-seal-lost",
        AirlockStallReason.NotSealing => "airlock-controller-stall-not-sealing",
        AirlockStallReason.MissingDoors => "airlock-controller-stall-missing-doors",
        AirlockStallReason.NotOpening => "airlock-controller-stall-not-opening",
        _ => "airlock-controller-stall-not-progressing",
    };

    public static string CycleKey(AirlockSide side) => side == AirlockSide.B
        ? "airlock-controller-ui-cycle-b"
        : "airlock-controller-ui-cycle-a";

    /// <summary>
    ///     A cancel already asked for becomes the emergency release.
    /// </summary>
    public static string CancelKey(bool cancelRequested) => cancelRequested
        ? "airlock-controller-ui-cancel-emergency"
        : "airlock-controller-ui-cancel";
}

/// <summary>
///     Appearance keys.
/// </summary>
[Serializable, NetSerializable]
public enum AirlockControllerVisuals : byte
{
    State, //AirlockCycleState
    Display, // AirlockControllerDisplay
    Error, // bool
    Cycling, // bool
}

/// <summary>
///     Shows current side or Maint mode
/// </summary>
[Serializable, NetSerializable]
public enum AirlockControllerDisplay : byte
{
    SideA,
    SideB,
    Maintenance,
}

[Serializable, NetSerializable]
public enum AirlockControllerVisualLayers : byte
{
    Base,
    State,
    Display,
    Error,
    Light,
}

[Serializable, NetSerializable]
public enum AirlockControllerUiKey : byte
{
    /// <summary>
    ///     Public UI
    /// </summary>
    Key,

    /// <summary>
    ///     Device config for atmosians
    /// </summary>
    Config,
}
