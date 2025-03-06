using System.Linq;
using System.Reflection;
using Content.Shared.Attachments;
using Robust.Shared.Utility;

namespace Content.Server.Attachments;

public sealed partial class AttachmentSystem : SharedAttachmentSystem
{
    protected override object? GetComponentFieldInfo(Type type, string field)
    {
        FieldInfo propInfo;
        if (type.GetField(field) is { } propInfoNormal)
            propInfo = propInfoNormal;
        else if (type.GetField(char.ToUpper(field[0]) + field[1..]) is { } propInfoUpper) // Try harder
            propInfo = propInfoUpper;
        else if (type.GetFields() // Try even harder
                     .ToList()
                     .Find(fieldInfo => fieldInfo.HasCustomAttribute<DataFieldAttribute>()) is {} fieldInfoExisting
                 && fieldInfoExisting.GetCustomAttribute<DataFieldAttribute>()?.Tag == field)
        {

            propInfo = fieldInfoExisting;
        }
        else
        {
            throw new ArgumentException(
                $"Field '{field}' does not exist publicly in component type '{type}'");
        }

        return propInfo;
    }

    protected override void CopyComponentFields<T>(T source, ref T target, Type ComponentType, List<string> fields)
    {
        foreach (var field in fields)
        {
            if (GetComponentFieldInfo(ComponentType, field) is FieldInfo fieldInfo)
                fieldInfo.SetValue(target, fieldInfo.GetValue(source));
        }
    }
}
