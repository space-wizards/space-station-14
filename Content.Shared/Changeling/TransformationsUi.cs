using Robust.Shared.Serialization;

namespace Content.Shared.Changeling;

/// <summary>
/// Tell the client the transformations it has stored.
/// Does not update after extracting dna or absorbing someone, so don't just keep the window open.
/// </summary>
[Serializable, NetSerializable]
public sealed class TransformationsBoundUserInterfaceState : BoundUserInterfaceState
{
    public readonly List<string> Names;

    public TransformationsBoundUserInterfaceState(List<string> names)
    {
        Names = names;
    }
}

[Serializable, NetSerializable]
public enum ChangelingUiKey : byte
{
    Transform
}

/// <summary>
/// Tell the server we want to transform into a stored transformation.
/// </summary>
[Serializable, NetSerializable]
public sealed class ChangelingTransformMessage : BoundUserInterfaceMessage
{
    public readonly string Name;

    public ChangelingTransformMessage(string name)
    {
        Name = name;
    }
}
