using Robust.Shared.Serialization;

namespace Content.Shared.Holiday;

/// <summary>
/// Networked event to request date from server.
/// </summary>
[NetSerializable, Serializable]
public sealed class RequestWhatDateItIsEvent : EntityEventArgs
{
}

/// <summary>
/// Networked event to push date from server.
/// </summary>
[NetSerializable, Serializable]
public sealed class ProvideWhatDateItIsEvent : EntityEventArgs
{
    public DateTime Date;

    public ProvideWhatDateItIsEvent(DateTime date)
    {
        Date = date;
    }
}
