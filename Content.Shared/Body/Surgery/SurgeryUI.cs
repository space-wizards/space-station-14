using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.Body.Surgery;

// operation selection

[Serializable, NetSerializable]
public enum SelectOperationUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class SelectOperationUiState : BoundUserInterfaceState
{
    public readonly EntityUid Target;
    public readonly EntityUid User;
    public readonly EntityUid[] Parts;

    public SelectOperationUiState(EntityUid target, EntityUid user, EntityUid[] parts)
    {
        Target = target;
        User = user;
        Parts = parts;
    }
}

/// <summary>
/// Message used to add an OperationComponent to a body/part
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

// organ selection

[Serializable, NetSerializable]
public enum SelectOrganUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class SelectOrganUiState : BoundUserInterfaceState
{
    public readonly EntityUid Target;
    public readonly EntityUid[] Organs;

    public SelectOperationUiState(EntityUid target, EntityUid[] organs)
    {
        Target = target;
        Organs = organs;
    }
}

/// <summary>
/// Message used to select an organ for extraction in an operation
/// </summary>
[Serializable, NetSerializable]
public sealed class OrganSelectedMessage : BoundUserInterfaceMessage
{
    // TODO SURGERY: should this be here??? feels kinda sus
    public readonly EntityUid Target;

    public readonly EntityUid Organ;

    public OrganSelectedMessage(EntityUid target, EntityUid organ)
    {
        Target = target;
        Organ = organ;
    }
}
