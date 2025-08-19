using Content.Shared.Damage;
using Content.Shared.Storage;
using Content.Shared.Tag;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Starlight.Weapon.Components;
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class WeaponDismantleOnShootComponent : Component
{
    [DataField, AutoNetworkedField]
    public float DismantleChance = 0.0f;

    /// <summary>
    /// How far to throw things when dismantling.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float DismantleDistance = 5f;

    [DataField]
    public SoundCollectionSpecifier? DismantleSound = new SoundCollectionSpecifier("MetalBreak");

    [DataField, AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    public DamageSpecifier SelfDamage = new();

    [DataField, AutoNetworkedField]
    public List<DismantleOnShootItem> items = [];
}

[DataDefinition]
[Serializable, NetSerializable]
public sealed partial class DismantleOnShootItem
{
    public DismantleOnShootItem() { }
    [DataField("id")]
    public EntProtoId? PrototypeId = null;

    /// <summary>
    ///     The probability that an item will spawn. Takes decimal form so 0.05 is 5%, 0.50 is 50% etc.
    /// </summary>
    [DataField("prob")]
    public float SpawnProbability = 1;

    [DataField]
    public int Amount = 1;

    [ViewVariables(VVAccess.ReadWrite), DataField]
    public Angle LaunchAngle = Angle.FromDegrees(0);

    [ViewVariables(VVAccess.ReadWrite), DataField]
    public Angle AngleRandomness = Angle.FromDegrees(5);
}
