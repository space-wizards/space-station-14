using Content.Shared.Attachments;

namespace Content.Client.Attachments;

public sealed partial class AttachmentSystem : SharedAttachmentSystem
{
    protected override void CopyComponentFields<T>(T source, ref T target, Type ComponentType, List<string> fields)
    {
        // boo
    }

    protected override object? GetComponentFieldInfo(Type type, string field)
    {
        // boo
        return null;
    }
}
