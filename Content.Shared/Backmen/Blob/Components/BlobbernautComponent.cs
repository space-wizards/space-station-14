using Content.Shared.Backmen.Blob;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared.Backmen.Blob.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true), Access(typeof(SharedBlobbernautSystem))]
public sealed partial class BlobbernautComponent : Component
{
    [DataField("color"), AutoNetworkedField]
    [Access(Other = AccessPermissions.ReadWrite)]
    public Color Color = Color.White;

    [ViewVariables(VVAccess.ReadWrite), DataField("damageFrequency")]
    public float DamageFrequency = 5;

    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan NextDamage = TimeSpan.Zero;

    [ViewVariables(VVAccess.ReadOnly), DataField("damage")]
    public DamageSpecifier Damage = new()
    {
        DamageDict = new Dictionary<string, FixedPoint2>
        {
            { "Piercing", 25 },
        }
    };

    [ViewVariables(VVAccess.ReadOnly)]
    public bool IsDead = false;

    [ViewVariables(VVAccess.ReadOnly)]
    [Access(Other = AccessPermissions.ReadWrite)]
    public EntityUid? Factory = default!;
}
