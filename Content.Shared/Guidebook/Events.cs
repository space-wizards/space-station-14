using Robust.Shared.Serialization;

namespace Content.Shared.Guidebook;

[Serializable, NetSerializable]
public sealed class RequestGuidebookDataEvent : EntityEventArgs { }

[Serializable, NetSerializable]
public sealed class UpdateGuidebookDataEvent : EntityEventArgs
{
    public GuidebookData Data;

    public UpdateGuidebookDataEvent(GuidebookData data)
    {
        Data = data;
    }
}
