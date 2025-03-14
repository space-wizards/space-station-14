using Robust.Shared.Serialization;

namespace Content.Shared._Impstation.CosmicCult;

[Serializable, NetSerializable]
public sealed partial class CosmicSiphonIndicatorEvent : EntityEventArgs
{
    public NetEntity Target = new();

    public CosmicSiphonIndicatorEvent(NetEntity target)
    {
        Target = target;
    }

    public CosmicSiphonIndicatorEvent() : this(new())
    {
    }
}
