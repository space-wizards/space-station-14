using Robust.Shared.Serialization;

namespace Content.Shared.GameTicking
{
    [Serializable, NetSerializable]
    public sealed class IncrementStatsValueEvent : EntityEventArgs
    {
        public string Key { get; }

        public IncrementStatsValueEvent(string key)
        {
            Key = key;
        }

    }
}
