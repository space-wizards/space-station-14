using Content.Shared.Damage;
using Content.Shared.Physics;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Weapons.Ranged;

[Prototype("hitscan")]
public sealed class HitscanPrototype : IPrototype, IShootable
{
    [ViewVariables]
    [IdDataFieldAttribute]
    public string ID { get; } = default!;

    [ViewVariables(VVAccess.ReadWrite), DataField("damage")]
    public DamageSpecifier? Damage;

    [ViewVariables(VVAccess.ReadOnly), DataField("muzzleFlash")]
    public SpriteSpecifier? MuzzleFlash;

    [ViewVariables(VVAccess.ReadOnly), DataField("travelFlash")]
    public SpriteSpecifier? TravelFlash;

    [ViewVariables(VVAccess.ReadOnly), DataField("impactFlash")]
    public SpriteSpecifier? ImpactFlash;

    [ViewVariables, DataField("collisionMask")]
    public int CollisionMask = (int) CollisionGroup.Opaque;

    [ViewVariables(VVAccess.ReadWrite), DataField("color")]
    public Color Color = Color.White;

    /// <summary>
    /// Try not to set this too high.
    /// </summary>
    [ViewVariables, DataField("maxLength")]
    public float MaxLength = 20f;
}
