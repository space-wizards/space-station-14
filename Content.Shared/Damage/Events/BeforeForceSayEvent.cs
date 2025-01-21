using Content.Shared.Dataset;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Damage.Events;

[Serializable, NetSerializable]
public sealed class BeforeForceSayEvent(ProtoId<LocalizedDatasetPrototype> prefixDataset) : EntityEventArgs
{
    public ProtoId<LocalizedDatasetPrototype> Prefix = prefixDataset;
}
