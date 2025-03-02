using Content.Shared.Attachments;

namespace Content.Client.Attachments;

public sealed partial class AttachmentSystem : SharedAttachmentSystem
{
    protected override void CopyComponentFields<T>(T source, ref T target, Type ComponentType, List<string> fields)
    {
        // boo
    }
}
