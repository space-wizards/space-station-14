using System.Linq;
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
            FieldInfo propInfo;
            if (ComponentType.GetField(field) is { } propInfoNormal)
                propInfo = propInfoNormal;
            else if (ComponentType.GetField(char.ToUpper(field[0]) + field[1..]) is { } propInfoUpper) // Try harder
                propInfo = propInfoUpper;
            else if (ComponentType.GetFields()
                         .ToList()
                         .Find(fieldInfo => fieldInfo.HasCustomAttribute<DataFieldAttribute>()) is {} fieldInfoExisting
                     && fieldInfoExisting.GetCustomAttribute<DataFieldAttribute>()!.Tag == field) // Try even harder
            {
                // Try even harder
                propInfo = fieldInfoExisting;
            }
            
            else
            {
                throw new ArgumentException(
                    $"Field '{field}' does not exist publicly in component type '{ComponentType}'");
            }
            propInfo.SetValue(target, propInfo.GetValue(source));
        }
    }
}
