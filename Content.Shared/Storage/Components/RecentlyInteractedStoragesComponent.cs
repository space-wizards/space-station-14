using Content.Shared.Storage.EntitySystems;
using Robust.Shared.Analyzers;
using Robust.Shared.GameStates;

namespace Content.Shared.Storage.Components;

/// <summary>
///     Attached to an actor to keep track of which storages it's opened in which order in order to close inventories on a LIFO basis if a new one past the limit is opened
/// </summary>
[RegisterComponent, Access(typeof(SharedStorageSystem)), AutoGenerateComponentState, NetworkedComponent]
public sealed partial class RecentlyOpenedStoragesComponent : Component
{
    /// <summary>
    ///     A list of lists of entities whose storages this actor has opened. Nested inventories (e.g. folder in a briefcase) belong to the same sublist.
    /// </summary>
    [DataField, ViewVariables, AutoNetworkedField]
    public List<List<NetEntity>> OpenedStorages = new();
}
