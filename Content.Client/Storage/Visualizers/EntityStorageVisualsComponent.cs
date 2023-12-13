namespace Content.Client.Storage.Visualizers;

[RegisterComponent]
[Access(typeof(EntityStorageVisualizerSystem))]
public sealed partial class EntityStorageVisualsComponent : Component
{
    /// <summary>
    /// The RSI state used for the base layer of the storage entity sprite while the storage is closed.
    /// </summary>
    [DataField("stateBaseClosed")]
    [ViewVariables(VVAccess.ReadWrite)]
    public string? StateBaseClosed;

    /// <summary>
    /// The RSI state used for the base layer of the storage entity sprite while the storage is open.
    /// </summary>
    [DataField("stateBaseOpen")]
    [ViewVariables(VVAccess.ReadWrite)]
    public string? StateBaseOpen;

    /// <summary>
    /// The RSI state used for the door/lid while the storage is open.
    /// </summary>
    [DataField("stateDoorOpen")]
    [ViewVariables(VVAccess.ReadWrite)]
    public string? StateDoorOpen;

    /// <summary>
    /// The RSI state used for the door/lid while the storage is closed.
    /// </summary>
    [DataField("stateDoorClosed")]
    [ViewVariables(VVAccess.ReadWrite)]
    public string? StateDoorClosed;

    /// <summary>
    /// The RSI state used for the lock indicator while the storage is locked.
    /// </summary>
    [DataField("stateLocked")]
    [ViewVariables(VVAccess.ReadWrite)]
    public string? StateLocked = "locked";

    /// <summary>
    /// The RSI state used for the lock indicator while the storage is unlocked.
    /// </summary>
    [DataField("stateUnlocked")]
    [ViewVariables(VVAccess.ReadWrite)]
    public string? StateUnlocked = "unlocked";

    /// <summary>
    /// The drawdepth the object has when it's open
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public int? OpenDrawDepth;

    /// <summary>
    /// The drawdepth the object has when it's closed
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public int? ClosedDrawDepth;
}
