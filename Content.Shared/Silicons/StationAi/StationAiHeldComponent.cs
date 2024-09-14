using Robust.Shared.GameStates;
using System.Threading;

namespace Content.Shared.Silicons.StationAi;

/// <summary>
/// Indicates this entity is currently held inside of a station AI core.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class StationAiHeldComponent : Component
{
    //TODO: Figure out if there is a better place to store all that
    public EntityUid? lastFollowedEntity = null;
    public CancellationTokenSource cancelRecaptureTokens = new CancellationTokenSource();
    public bool lostFollowed = false;
}
