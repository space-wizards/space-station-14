using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using System;

namespace Content.Shared.Body.Surgery.UI;

[Serializable, NetSerializable]
public sealed class SurgeryUIState : BoundUserInterfaceState
{
    public readonly EntityUid Target;
    public readonly EntityUid User;
    public readonly EntityUid[] Entities;

    public SurgeryUIState(EntityUid target, EntityUid user, EntityUid[] entities)
    {
        Target = target;
        User = user;
        Entities = entities;
    }
}
