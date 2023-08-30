using Robust.Shared.Serialization;

namespace Content.Shared.Damage
{
    [Serializable, NetSerializable]
    public enum DamageVisualizerKeys
    {
        Disabled,
        DamageSpecifierDelta,
        DamageUpdateGroups,
        ForceUpdate
    }

    [Serializable, NetSerializable]
    public sealed class DamageVisualizerGroupData : ICloneable
    {
        public List<string> GroupList;

        public DamageVisualizerGroupData(List<string> groupList)
        {
            GroupList = groupList;
        }

        public object Clone()
        {
            return new DamageVisualizerGroupData(new List<string>(GroupList));
        }
    }
}
