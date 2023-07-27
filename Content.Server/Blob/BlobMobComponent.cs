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
            { "Blunt", -1 },
            { "Slash", -1 },
            { "Piercing", -1 },
            { "Heat", -1 },
            { "Cold", -1 },
            { "Shock", -1 },
        }
    };
}
