using Robust.Shared.GameStates;

namespace Content.Shared.Objectives.Components;

/// <summary>
/// An abstract component that allows other systems to count adjacent objects as "stolen" when controlling other systems
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class StealAreaComponent : Component
{
    /// <summary>
    /// Is the component currently enabled?
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Enabled = true;

    /// <summary>
    /// The range to check for items in.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Range = 1f;

    /// <summary>
    /// All the minds that will be credited with stealing from this area.
    /// </summary>
    /// <remarks>
    /// TODO: Network this when we have WeakEntityReference.
    /// </remarks>
    [DataField]
    public HashSet<EntityUid> Owners = new();

    /// <summary>
    /// The count of the owner hashset.
    /// This is a separate datafield because networking the list would cause PVS errors if an entity inside would be deleted and networked.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int OwnerCount = 0;
}
