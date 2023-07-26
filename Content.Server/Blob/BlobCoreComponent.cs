using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio;

namespace Content.Server.Blob;

[RegisterComponent]
public sealed class BlobCoreComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite), DataField("attackRate")]
    public float ActionRate = 0.4f;

    [DataField("attackSound")]
    public SoundSpecifier AttackSound = new SoundPathSpecifier("/Audio/Animals/Blob/blobattack.ogg");

    [ViewVariables(VVAccess.ReadOnly), DataField("damage")]
    public DamageSpecifier Damage = new()
    {
        DamageDict = new Dictionary<string, FixedPoint2>
        {
            { "Blunt", 15 },
            { "Structural", 100 },
        }
    };

    [ViewVariables(VVAccess.ReadWrite), DataField("factoryRadiusLimit")]
    public float FactoryRadiusLimit = 6f;

    [ViewVariables(VVAccess.ReadWrite), DataField("resourceRadiusLimit")]
    public float ResourceRadiusLimit = 3f;

    [ViewVariables(VVAccess.ReadWrite), DataField("nodeRadiusLimit")]
    public float NodeRadiusLimit = 4f;

    [ViewVariables(VVAccess.ReadWrite), DataField("attackCost")]
    public FixedPoint2 AttackCost = 2;

    [ViewVariables(VVAccess.ReadWrite), DataField("factoryBlobCost")]
    public FixedPoint2 FactoryBlobCost = 60;

    [ViewVariables(VVAccess.ReadWrite), DataField("normalBlobCost")]
    public FixedPoint2 NormalBlobCost = 4;

    [ViewVariables(VVAccess.ReadWrite), DataField("resourceBlobCost")]
    public FixedPoint2 ResourceBlobCost = 40;

    [ViewVariables(VVAccess.ReadWrite), DataField("nodeBlobCost")]
    public FixedPoint2 NodeBlobCost = 50;

    [ViewVariables(VVAccess.ReadWrite), DataField("blobbernautCost")]
    public FixedPoint2 BlobbernautCost = 60;

    [ViewVariables(VVAccess.ReadWrite), DataField("strongBlobCost")]
    public FixedPoint2 StrongBlobCost = 15;

    [ViewVariables(VVAccess.ReadWrite), DataField("reflectiveBlobCost")]
    public FixedPoint2 ReflectiveBlobCost = 15;

    [ViewVariables(VVAccess.ReadWrite), DataField("splitCoreCost")]
    public FixedPoint2 SplitCoreCost = 100;

    [ViewVariables(VVAccess.ReadWrite), DataField("swapCoreCost")]
    public FixedPoint2 SwapCoreCost = 80;

    [ViewVariables(VVAccess.ReadWrite), DataField("swapChemCost")]
    public FixedPoint2 SwapChemCost = 40;

    [ViewVariables(VVAccess.ReadWrite), DataField("reflectiveBlobTile")]
    public string ReflectiveBlobTile = "ReflectiveBlobTile";

    [ViewVariables(VVAccess.ReadWrite), DataField("strongBlobTile")]
    public string StrongBlobTile = "StrongBlobTile";

    [ViewVariables(VVAccess.ReadWrite), DataField("normalBlobTile")]
    public string NormalBlobTile = "NormalBlobTile";

    [ViewVariables(VVAccess.ReadWrite), DataField("factoryBlobTile")]
    public string FactoryBlobTile = "FactoryBlobTile";

    [ViewVariables(VVAccess.ReadWrite), DataField("resourceBlobTile")]
    public string ResourceBlobTile = "ResourceBlobTile";

    [ViewVariables(VVAccess.ReadWrite), DataField("nodeBlobTile")]
    public string NodeBlobTile = "NodeBlobTile";

    [ViewVariables(VVAccess.ReadWrite), DataField("coreBlobTile")]
    public string CoreBlobTile = "CoreBlobTile";

    [ViewVariables(VVAccess.ReadOnly)]
    public EntityUid? Observer = default!;

    [ViewVariables(VVAccess.ReadOnly)]
    public List<EntityUid> BlobTiles = new();

    public TimeSpan NextAction = TimeSpan.Zero;

    [ViewVariables(VVAccess.ReadWrite)]
    public FixedPoint2 Points = 50;
}
