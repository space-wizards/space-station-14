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
        public EntityUid Target { get; }
        public string Title { get; }
        public string From { get; }
        public string Content { get; }
        public string StampState { get; }

        public Send(EntityUid target, string title, string from, string content, string stamp)
        {
            Target = target;
            Title = title;
            From = from;
            Content = content;
            StampState = stamp;
        }
    }
}
