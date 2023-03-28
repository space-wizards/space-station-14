using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.Body.Surgery.Components;

/// <summary>
/// Used to select an organ for extraction
/// </summary>
[Serializable, NetSerializable]
public sealed class OrganSelectedMessage : BoundUserInterfaceMessage
{
    public readonly EntityUid Organ;

    public OrganSelectedMessage(EntityUid organ)
    {
        Organ = organ;
    }
}
