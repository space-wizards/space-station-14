using Content.Shared.Damage;
using Content.Shared.FixedPoint;

namespace Content.Server.Blob;

[RegisterComponent]
public sealed class BlobbernautComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite), DataField("damageFrequency")]
    public float DamageFrequency = 5;

    [ViewVariables(VVAccess.ReadOnly), DataField("damage")]
    public DamageSpecifier Damage = new()
    {
        DamageDict = new Dictionary<string, FixedPoint2>
        {
            { "Piercing", 15 },
        }
    };

    [ViewVariables(VVAccess.ReadOnly)]
    public bool IsDead = false;

    [ViewVariables(VVAccess.ReadOnly)]
    public EntityUid? Factory = default!;

    public float Accumulator = 0;
}
