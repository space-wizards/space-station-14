using Robust.Shared.Serialization;

namespace Content.Shared.Renameable;

[Serializable, NetSerializable]
public enum RenamingUiKey
{
    Key
}

[Serializable, NetSerializable]
public sealed class RenamedMessage : BoundUserInterfaceMessage
{
    /// <summary>
    /// The new name the client sent, will be edited by the server before setting the entity's name.
    /// </summary>
    public string Name;

    public RenamedMessage(string name)
    {
        Name = name;
    }
}
