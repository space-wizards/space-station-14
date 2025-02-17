using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Backmen.Blob.Components;

[RegisterComponent]
public sealed partial class BlobCoreComponent : Component
{
    [DataField]
    public EntProtoId MindRoleBlobPrototypeId = "MindRoleBlob";

    [ViewVariables(VVAccess.ReadWrite), DataField("attackRate")]
    public float AttackRate = 0.8f;

    [ViewVariables(VVAccess.ReadWrite), DataField("returnResourceOnRemove")]
    public float ReturnResourceOnRemove = 0.3f;

    [ViewVariables(VVAccess.ReadWrite), DataField("canSplit")]
    public bool CanSplit = true;

    [DataField("attackSound")]
    public SoundSpecifier AttackSound = new SoundPathSpecifier("/Audio/Animals/Blob/blobattack.ogg");

    [ViewVariables(VVAccess.ReadWrite)]
    public Dictionary<BlobChemType, DamageSpecifier> ChemDamageDict { get; set; } = new()
    {
        {
            BlobChemType.BlazingOil, new DamageSpecifier()
            {
                DamageDict = new Dictionary<string, FixedPoint2>
                {
                    { "Heat", 15 },
                    { "Structural", 150 },
                }
            }
        },
        {
            BlobChemType.ReactiveSpines, new DamageSpecifier()
            {
                DamageDict = new Dictionary<string, FixedPoint2>
                {
                    { "Blunt", 8 },
                    { "Slash", 8 },
                    { "Piercing", 8 },
                    { "Structural", 150 },
                }
            }
        },
        {
            BlobChemType.ExplosiveLattice, new DamageSpecifier()
            {
                DamageDict = new Dictionary<string, FixedPoint2>
                {
                    { "Heat", 5 },
                    { "Structural", 150 },
                }
            }
        },
        {
            BlobChemType.ElectromagneticWeb, new DamageSpecifier()
            {
                DamageDict = new Dictionary<string, FixedPoint2>
                {
                    { "Structural", 150 },
                    { "Heat", 20 },
                },
            }
        },
        {
            BlobChemType.RegenerativeMateria, new DamageSpecifier()
            {
                DamageDict = new Dictionary<string, FixedPoint2>
                {
                    { "Structural", 150 },
                    { "Poison", 15 },
                }
            }
        },
    };

    [ViewVariables(VVAccess.ReadOnly)]
    public readonly Dictionary<BlobChemType, Color> ChemСolors = new()
    {
        {BlobChemType.ReactiveSpines, Color.FromHex("#637b19")},
        {BlobChemType.BlazingOil, Color.FromHex("#937000")},
        {BlobChemType.RegenerativeMateria, Color.FromHex("#441e59")},
        {BlobChemType.ExplosiveLattice, Color.FromHex("#6e1900")},
        {BlobChemType.ElectromagneticWeb, Color.FromHex("#0d7777")},
    };

    [ViewVariables(VVAccess.ReadOnly), DataField("blobExplosive")]
    public string BlobExplosive = "Blob";

    [ViewVariables(VVAccess.ReadOnly), DataField("defaultChem")]
    public BlobChemType DefaultChem = BlobChemType.ReactiveSpines;

    [ViewVariables(VVAccess.ReadOnly), DataField("currentChem")]
    public BlobChemType CurrentChem = BlobChemType.ReactiveSpines;

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
    public string CoreBlobTile = "CoreBlobTileGhostRole";

    [ViewVariables(VVAccess.ReadWrite), DataField("coreBlobTotalHealth")]
    public FixedPoint2 CoreBlobTotalHealth = 400;

    [ViewVariables(VVAccess.ReadWrite),
     DataField("ghostPrototype", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string ObserverBlobPrototype = "MobObserverBlob";

    [DataField("greetSoundNotification")]
    public SoundSpecifier GreetSoundNotification = new SoundPathSpecifier("/Audio/Effects/clang.ogg");

    [ViewVariables(VVAccess.ReadOnly)]
    public EntityUid? Observer = default!;

    [ViewVariables(VVAccess.ReadOnly)]
    public List<EntityUid> BlobTiles = new();

    public TimeSpan NextAction = TimeSpan.Zero;

    [ViewVariables(VVAccess.ReadWrite)]
    public FixedPoint2 Points = 0;

    [DataField("actionSwapBlobChem")]
    public EntityUid? ActionSwapBlobChem = null;

    [DataField("actionTeleportBlobToCore")]
    public EntityUid? ActionTeleportBlobToCore = null;

    [DataField("actionTeleportBlobToNode")]
    public EntityUid? ActionTeleportBlobToNode = null;

    [DataField("actionCreateBlobFactory")]
    public EntityUid? ActionCreateBlobFactory = null;

    [DataField("actionCreateBlobResource")]
    public EntityUid? ActionCreateBlobResource = null;

    [DataField("actionCreateBlobNode")]
    public EntityUid? ActionCreateBlobNode = null;

    [DataField("actionCreateBlobbernaut")]
    public EntityUid? ActionCreateBlobbernaut = null;

    [DataField("actionSplitBlobCore")]
    public EntityUid? ActionSplitBlobCore = null;
    
    [DataField("actionSwapBlobCore")]
    public EntityUid? ActionSwapBlobCore = null;
}

[Serializable, NetSerializable]
public enum BlobChemType : byte
{
    BlazingOil,
    ReactiveSpines,
    RegenerativeMateria,
    ExplosiveLattice,
    ElectromagneticWeb
}
