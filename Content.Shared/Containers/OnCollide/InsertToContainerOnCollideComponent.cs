using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared.Containers.OnCollide;

/// <summary>
/// When this component is added, we insert to a given container any entity we collide with
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(InsertToContainerOnCollideSystem))]
public sealed class InsertToContainerOnCollideComponent : Component
{
    /// <summary>
    /// ID of the target container
    /// </summary>
    [DataField("container", required: true)]
    [ViewVariables]
    public string Container = default!;

    [DataField("insertableEntities")]
    public EntityWhitelist? InsertableEntities;

    /// <summary>
    /// The minimum velocity we have to have to be able to insert something in the container.
    /// Represented in meters/tiles per second
    /// </summary>
    [DataField("requiredVelocity")]
    [ViewVariables(VVAccess.ReadWrite)]
    public float RequiredVelocity;

    /// <summary>
    /// Entities which we should never insert on collide
    /// </summary>
    [DataField("blacklistedEntities")]
    public EntityWhitelist? BlacklistedEntities;
}
