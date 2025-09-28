using Robust.Shared.GameStates;

namespace Content.Shared._Starlight.Storage;

/// <summary>
/// Component that defines various variables used in our StorageActionSystem, namely delay of opening, and popup texts
/// </summary>
[RegisterComponent, Access(typeof(SharedPrivateStorageSystem))]
public sealed partial class PrivateStorageComponent : Component
{
    /// <summary>
    /// The amount of time it takes to open the storage for an outsider
    /// </summary>
    [DataField("accessDelay")] public TimeSpan AccessDelay = TimeSpan.FromSeconds(5);

    /// <summary>
    /// A popup that will be shown when storage is accesses by an outsider
    /// </summary>
    [DataField("accessPopup")] public string AccessPopup = "action-storage-accessing-outsider";
}
