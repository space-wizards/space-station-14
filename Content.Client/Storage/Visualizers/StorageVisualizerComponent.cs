namespace Content.Client.Storage.Visualizers;

[RegisterComponent]
[Access(typeof(EntityStorageVisualizerSystem))]
public sealed class EntityStorageVisualsComponent : Component
{
    /// <summary>
    /// The RSI state used for the base layer of the storage entity sprite while the storage is closed.
    /// </summary>
    [DataField("stateBase")]
    [ViewVariables(VVAccess.ReadWrite)]
    public string? StateBase;

    /// <summary>
    /// The RSI state used for the base layer of the storage entity sprite while the storage is open.
    /// </summary>
    [DataField("stateBaseOpen")]
    [ViewVariables(VVAccess.ReadWrite)]
    public string? StateBaseOpen;

    /// <summary>
    /// The RSI state used for the door/lid while the storage is open.
    /// </summary>
    [DataField("stateOpen")]
    [ViewVariables(VVAccess.ReadWrite)]
    public string? StateOpen;

    /// <summary>
    /// The RSI state used for the door/lid while the storage is closed.
    /// </summary>
    [DataField("stateClosed")]
    [ViewVariables(VVAccess.ReadWrite)]
    public string? StateClosed;

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
}
