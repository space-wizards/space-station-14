using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared.Fax;

[Serializable, NetSerializable]
public sealed class AdminFaxEuiState : EuiStateBase
{
    public List<AdminFaxEntry> Entries { get; }

    public AdminFaxEuiState(List<AdminFaxEntry> entries)
    {
        Entries = entries;
    }
}

[Serializable, NetSerializable]
public sealed class AdminFaxEntry
{
    public EntityUid Uid { get; }
    public string Name { get; }
    public string Address { get; }

    public AdminFaxEntry(EntityUid uid, string name, string address)
    {
        Uid = uid;
        Name = name;
        Address = address;
    }
}

public static class AdminFaxEuiMsg
{
    [Serializable, NetSerializable]
    public sealed class Close : EuiMessageBase
    {
    }

    [Serializable, NetSerializable]
    public sealed class Follow : EuiMessageBase
    {
        public EntityUid TargetFax { get; }

        public Follow(EntityUid targetFax)
        {
            TargetFax = targetFax;
        }
    }

    [Serializable, NetSerializable]
    public sealed class Send : EuiMessageBase
    {
        public EntityUid TargetFax { get; }
        public string Name { get; }
        public string Content { get; }

        public Send(EntityUid targetFax, string name, string content)
        {
            TargetFax = targetFax;
            Name = name;
            Content = content;
        }
    }
}
