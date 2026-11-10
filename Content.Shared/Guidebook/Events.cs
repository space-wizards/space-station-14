using Robust.Shared.Serialization;

namespace Content.Shared.Guidebook;

/// <summary>
/// Sends all extracted prototype data needed by GuidebookDataSystem.
/// Raised by the server directed at newly-connected clients.
/// Also raised by the server at ALL clients when prototype data is hot-reloaded.
/// </summary>
[Serializable, NetSerializable]
public sealed class UpdateGuidebookDataEvent : EntityEventArgs
{
    public GuidebookData Data;

    public UpdateGuidebookDataEvent(GuidebookData data)
    {
        Data = data;
    }
}
