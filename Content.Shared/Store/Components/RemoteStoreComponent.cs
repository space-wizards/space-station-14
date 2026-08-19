using Robust.Shared.GameStates;

namespace Content.Shared.Store.Components;

/// <summary>
/// This component manages a store which players can use to purchase different listings
/// through the ui. The currency, listings, and categories are defined in yaml.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class RemoteStoreComponent : Component
{
    /// <summary>
    /// The store which is currently being targetted for remote opening
    /// </summary>
    [DataField]
    public EntityUid? Store;
}
