using System.Reflection;
using Content.Shared.Attachments;
using Robust.Shared.Utility;

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
            {
                foreach (var fieldinfo in ComponentType.GetFields())
                {
                    if (fieldinfo.HasCustomAttribute<DataFieldAttribute>())
                    {
                        var dataFieldAttr = fieldinfo.GetCustomAttribute<DataFieldAttribute>();
                        if (dataFieldAttr?.Tag is {})
                            throw new ArgumentException(
                                $"'{field}' is not a field in component {ComponentType}! Did you mean '{dataFieldAttr.Tag}'?");
                        else
                            throw new ArgumentException(
                                $"'{field}' is not a field in component {ComponentType}! Did you forget to capitalize it?");
                    }
                }
                throw new ArgumentException(
                    $"'{field}' is not a field or DataField in component {ComponentType}! Did you type it correctly?");
            }
        }
    }
}
