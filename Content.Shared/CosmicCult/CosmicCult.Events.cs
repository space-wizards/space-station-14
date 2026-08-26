using Robust.Shared.Audio;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.CosmicCult;

[Serializable, NetSerializable]
public sealed partial class SiphonVisualsEvent(NetEntity target) : EntityEventArgs
{
    public NetEntity Target = target;

    public SiphonVisualsEvent() : this(new())
    {
    }
}

[Serializable, NetSerializable]
public sealed partial class InfluenceVisualsEvent : EntityEventArgs
{
    public NetEntity Target;

    public NetEntity Monument;

    public SpriteSpecifier Icon;

    public SoundSpecifier GachaSound;

    public InfluenceVisualsEvent(NetEntity target, NetEntity monument, SpriteSpecifier icon, SoundSpecifier gachaSound)
    {
        Target = target;
        Monument = monument;
        GachaSound = gachaSound;
        Icon = icon;
    }
}
