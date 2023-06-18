// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Photocopier.Forms.FormManagerShared;

[Serializable, NetSerializable]
public sealed class RequestPhotocopierFormsMessage : EntityEventArgs
{
}

[Serializable, NetSerializable]
public sealed class PhotocopierFormsMessage : EntityEventArgs
{
    public Dictionary<string, Dictionary<string, FormGroup>> Data;

    public PhotocopierFormsMessage(Dictionary<string, Dictionary<string, FormGroup>> data)
    {
        Data = data;
    }
}

/// <summary>
/// Describes how server should try access the form
/// </summary>
[Serializable, NetSerializable]
public sealed class FormDescriptor
{
    public readonly string CollectionId;
    public readonly string GroupId;
    public readonly string FormId;

    public FormDescriptor(string collectionId, string groupId, string formId)
    {
        CollectionId = collectionId;
        GroupId = groupId;
        FormId = formId;
    }
}
