using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.Body.Surgery.UI;

/// <summary>
/// Used to add an OperationComponent to a body/part
/// </summary>
[Serializable, NetSerializable]
public sealed class OperationSelectedMessage : BoundUserInterfaceMessage
{
    // TODO SURGERY: should these be here??? feels kinda sus
    public readonly EntityUid Target;
    public readonly EntityUid User;

    public readonly EntityUid Part;
    public readonly string Operation;

    public OperationSelectedMessage(EntityUid target, EntityUid user, EntityUid part, string operation)
    {
        Target = target;
        User = user;
        Part = part;
        Operation = operation;
    }
}
