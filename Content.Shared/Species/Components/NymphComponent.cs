using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;
using Robust.Shared.GameStates;

namespace Content.Shared.Species.Components;
/// <summary>
/// This will replace one entity with another entity when it is removed from a body part.
/// Obviously hyper-specific. If you somehow find another use for this, good on you.
/// </summary>

[RegisterComponent, NetworkedComponent]
public sealed partial class NymphComponent : Component
{
    /// <summary>
    /// The entity to replace the organ with.
    /// </summary>
    [DataField(required: true)]
    public EntProtoId EntityPrototype = default!;

    /// <summary>
    /// Whether to transfer the mind to this new entity.
    /// </summary>
    [DataField]
    public bool TransferMind = false;

    /// <summary>
    /// Whitelist the owner of the organ needs to meet for the organ to turn into a nymph.
    /// Can be null.
    /// </summary>
    [DataField]
    public EntityWhitelist? Whitelist;

    /// <summary>
    /// Blacklist for the owner of the organ for it to turn into a nymph.
    /// Can be null.
    /// </summary>
    [DataField]
    public EntityWhitelist? Blacklist;
}
