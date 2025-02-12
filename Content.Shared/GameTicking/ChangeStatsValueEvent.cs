using Robust.Shared.Serialization;

namespace Content.Shared.GameTicking;

[Serializable, NetSerializable]
public sealed class ChangeStatsValueEvent(string key, int amount) : HandledEntityEventArgs
{
    public string Key = key;
    public int Amount = amount;
}
