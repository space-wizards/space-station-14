using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._Afterlight.ThirdDimension;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed class ZViewComponent : Component
{
    [ViewVariables]
    public List<EntityUid> DownViewEnts = new();
}

[Serializable, NetSerializable]
public sealed class ZViewComponentState : ComponentState
{
    public List<EntityUid> DownViewEnts;

    public ZViewComponentState(List<EntityUid> downViewEnts)
    {
        DownViewEnts = downViewEnts;
    }
}
