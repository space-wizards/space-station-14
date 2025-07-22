// Modifications ported by Ronstation from CorvaxNext, therefore this file is licensed as MIT sublicensed with AGPL-v3.0.
using Robust.Shared.GameStates;

namespace Content.Shared.Silicons.StationAi;

/// <summary>
/// Indicates this entity is currently held inside of a station AI core.
/// </summary>
[RegisterComponent, NetworkedComponent]
// Corvax-Next-AiRemoteControl-Start
public sealed partial class StationAiHeldComponent : Component
{
    [DataField]
    public EntityUid? CurrentConnectedEntity;
}
// Corvax-Next-AiRemoteControl-End
