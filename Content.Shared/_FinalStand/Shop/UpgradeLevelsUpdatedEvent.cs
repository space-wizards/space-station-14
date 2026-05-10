using Robust.Shared.Serialization;

namespace Content.Shared._FinalStand.Shop;

[Serializable, NetSerializable]
public sealed class UpgradeLevelsUpdatedEvent : EntityEventArgs
{
    public readonly Dictionary<string, int> Levels;

    public UpgradeLevelsUpdatedEvent(Dictionary<string, int> levels)
    {
        Levels = levels;
    }
}
