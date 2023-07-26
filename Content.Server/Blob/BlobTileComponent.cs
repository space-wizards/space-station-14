using Content.Shared.Blob;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;

namespace Content.Server.Blob;

[RegisterComponent]
public sealed class BlobTileComponent : SharedBlobTileComponent
{
    [ViewVariables(VVAccess.ReadOnly)]
    public EntityUid? Core = default!;

    [ViewVariables(VVAccess.ReadOnly), DataField("tileType")]
    public BlobTileType BlobTileType = BlobTileType.Normal;

    [ViewVariables(VVAccess.ReadOnly), DataField("blobBorder")]
    public string BlobBorder = "BlobBorder";

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

[Serializable]
public enum BlobTileType : byte
{
    Normal,
    Strong,
    Reflective,
    Resource,
    Storage,
    Node,
    Factory,
    Core,
    None,
}
