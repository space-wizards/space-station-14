using Robust.Shared.GameStates;
using Robust.Shared.Player;

namespace Content.Shared.Changeling.Components;

/// <summary>
/// Marker component for cloned identities devoured by a changeling.
/// These are stored on a paused map so that the changeling can transform into them.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ChangelingStoredIdentityComponent : Component
{
    /// <summary>
    /// The original entity the identity was cloned from.
    /// </summary>
    /// <remarks>
    /// TODO: Not networked at the moment because it will create PVS errors when the original is somehow deleted.
    /// Use WeakEntityReference once it's merged.
    /// </remarks>
    [DataField]
    public EntityUid? OriginalEntity;

    /// <summary>
    /// The player session of the original entity, if any.
    /// Used for admin logging purposes.
    /// </summary>
    [ViewVariables]
    public ICommonSession? OriginalSession;
}
