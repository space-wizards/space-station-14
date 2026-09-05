using Content.Shared.Hands.Components;
using Robust.Shared.GameStates;

namespace Content.Shared.Body;

/// <summary>
/// Organs with this component provide a hand with the given ID and data to the body when inserted.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(HandOrganSystem))]
public sealed partial class HandOrganComponent : Component
{
    /// <summary>
    /// The hand ID used by <seealso cref="HandsComponent" /> on the body
    /// </summary>
    [DataField(required: true)]
    public string HandID;

    /// <summary>
    /// The data used to create the hand
    /// </summary>
    [DataField(required: true)]
    public Hand Data;
}
