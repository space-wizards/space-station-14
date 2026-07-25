using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.DeadSpace.Flamethrower;

[RegisterComponent, NetworkedComponent]
public sealed partial class ContinuousFlamethrowerComponent : Component
{
    [DataField]
    public float MaxRange = 8f;

    [DataField]
    public float MinimumRange = 0.65f;

    [DataField]
    public float FuelPerTick = 0.6667f;

    [DataField]
    public float MaximumRangeFuelMultiplier = 1.75f;

    [DataField]
    public SoundSpecifier ShotSound = new SoundPathSpecifier("/Audio/_DeadSpace/Weapons/Guns/Gunshots/flamethrower.ogg");

    [DataField]
    public SoundSpecifier AmbientSound = new SoundPathSpecifier("/Audio/Effects/burning.ogg");
}

[RegisterComponent]
public sealed partial class FlamethrowerBurningComponent : Component
{
}

[RegisterComponent]
public sealed partial class FlamethrowerFuelTankComponent : Component
{
}

[Serializable, NetSerializable]
public sealed class FlamethrowerInputEvent(NetEntity weapon, NetCoordinates target, bool active) : EntityEventArgs
{
    public NetEntity Weapon = weapon;
    public NetCoordinates Target = target;
    public bool Active = active;
}

[Serializable, NetSerializable]
public sealed class FlamethrowerVisualEvent(List<NetCoordinates> points) : EntityEventArgs
{
    public List<NetCoordinates> Points = points;
}
