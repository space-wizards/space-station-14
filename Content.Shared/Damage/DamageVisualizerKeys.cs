using Content.Shared.Damage.Prototypes;
using Robust.Shared.Prototypes;
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
        public List<ProtoId<DamageGroupPrototype>> GroupList;

        public DamageVisualizerGroupData(List<ProtoId<DamageGroupPrototype>> groupList)
        {
            GroupList = groupList;
        }

        public object Clone()
        {
            return new DamageVisualizerGroupData(new List<ProtoId<DamageGroupPrototype>>(GroupList));
        }
    }
}
