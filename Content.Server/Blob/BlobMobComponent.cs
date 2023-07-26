using Content.Shared.Damage;

namespace Content.Server.Blob;

[RegisterComponent]
public sealed class BlobMobComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly), DataField("healthOfPulse")]
    public DamageSpecifier HealthOfPulse = new()
    {
        DamageDict = new ()
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
