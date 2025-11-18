using Robust.Shared.Serialization;


namespace Content.Shared.GameTicking
{
    [Serializable, NetSerializable]
    public sealed class RoundRestartCleanupEvent : EntityEventArgs
    {
    }
}
