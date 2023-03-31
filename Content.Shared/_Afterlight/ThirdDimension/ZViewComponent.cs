using Robust.Shared.Serialization;

namespace Content.Shared._Afterlight.ThirdDimension;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent]
public sealed class ZViewComponent : Component
{
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
