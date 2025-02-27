using Content.Shared.Attachments;

namespace Content.Server.Attachments;

public sealed partial class AttachmentSystem : SharedAttachmentSystem
{
    protected override void CopyComponentFields<T>(T source, ref T target, Type ComponentType, List<string> fields)
    {
        foreach (var field in fields)
        {
            if (ComponentType.GetField(field) is { } propInfo)
                propInfo.SetValue(target, propInfo.GetValue(source));
            else
                throw new ArgumentException($"'{field}' is not a field in component {ComponentType}!");
        }
    }
}
