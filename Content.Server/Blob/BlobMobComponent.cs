using Content.Shared.Damage;
using Content.Shared.FixedPoint;

namespace Content.Server.Blob;

[RegisterComponent]
public sealed class BlobMobComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly), DataField("healthOfPulse")]
    public DamageSpecifier HealthOfPulse = new()
    {
        DamageDict = new Dictionary<string, FixedPoint2>
        {
            { "Blunt", -2 },
            { "Slash", -2 },
            { "Piercing", -2 },
            { "Heat", -2 },
            { "Cold", -2 },
            { "Shock", -2 },
        }
    };
}
