using Robust.Shared.GameStates;
using Content.Shared.Atmos;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Stray.Weapons.FireUnderBullet;


[RegisterComponent]
[NetworkedComponent]
[Access(typeof(SharedFireUnderBulletSystem))]

public sealed partial class FireUnderBulletComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite), DataField("ruptureSound")]
    public SoundSpecifier RuptureSound = new SoundPathSpecifier("/Audio/_WH40K/Weapons/flamethrower.ogg");

    [ViewVariables(VVAccess.ReadWrite), DataField("pickedUp", required: true)]
    public bool pickedUp = true;

    [ViewVariables(VVAccess.ReadWrite), DataField("releaseSpeed")]
    public float releaseSpeed = 1;

    [ViewVariables(VVAccess.ReadWrite), DataField("releaseTemp")]
    public float releaseTemp = 279;

    [ViewVariables(VVAccess.ReadWrite), DataField("releaseGas", required: true)]
    public GasMixture releaseGas {get; set;} = new();

    public TimeSpan minusTime = TimeSpan.Zero;
    public TimeSpan removeTime = TimeSpan.Zero;
    public TimeSpan startTime = TimeSpan.Zero;
}
